// Battle of the Brooms: the juicy finish on a station (docs 24 section 15.15). A completed task squashes the station for
// 0.35 s on the curve 1 -> (1.15, 0.85, 1.15) -> (0.95, 1.08, 0.95) -> 1, bursts golden stars over it and sweeps a
// sheen across the surfaces through the existing _Sheen property. The station root sits on the floor, so the squash
// reads as the counter taking the hit rather than a model scaling around its centre.
//
// DioramaAmbient adds one of these to every TaskSpot; it listens to the spot's own Closed event, so no gameplay code
// had to change: a two-pass first pass and a sabotage close report completed = false and stay quiet.
using System.Collections.Generic;
using UnityEngine;

namespace Bob.TaskRace
{
    [DisallowMultipleComponent]
    public class BobStationJuice : MonoBehaviour
    {
        [Tooltip("Length of the squash curve, seconds.")]
        public float duration = 0.35f;
        [Tooltip("Golden stars in the burst.")]
        public int sparkCount = 22;

        static readonly int SheenId = Shader.PropertyToID("_Sheen");

        TaskSpot spot;
        Vector3 rest;
        float squashT = -1f;
        float sheenT = -1f;
        float sheenTime = 0.45f;
        Renderer[] shells;
        MaterialPropertyBlock block;
        ParticleSystem sparks;

        void Awake()
        {
            spot = GetComponent<TaskSpot>();
            rest = transform.localScale;
            block = new MaterialPropertyBlock();
            var list = new List<Renderer>();
            foreach (var r in GetComponentsInChildren<MeshRenderer>(true))
                if (r.sharedMaterial != null && r.sharedMaterial.HasProperty(SheenId)) list.Add(r);
            shells = list.ToArray();
        }

        void OnEnable()
        {
            if (spot != null) spot.Closed += OnClosed;
        }

        void OnDisable()
        {
            if (spot != null) spot.Closed -= OnClosed;
        }

        void OnClosed(TaskSpot s, bool completed)
        {
            if (!completed || s.State != SpotState.Clean) return;
            squashT = 0f;
            sheenT = 0f;
            Burst();
        }

        void Update()
        {
            if (squashT >= 0f)
            {
                squashT += Time.deltaTime;
                float u = Mathf.Clamp01(squashT / duration);
                transform.localScale = Vector3.Scale(rest, Stretch(u));
                if (u >= 1f) { squashT = -1f; transform.localScale = rest; }
            }

            if (sheenT >= 0f)
            {
                sheenT += Time.deltaTime;
                float u = Mathf.Clamp01(sheenT / sheenTime);
                float sheen = Mathf.Sin(u * Mathf.PI) * 0.44f;
                for (int i = 0; i < shells.Length; i++)
                {
                    if (shells[i] == null) continue;
                    block.SetFloat(SheenId, sheen);
                    shells[i].SetPropertyBlock(block);
                }
                if (u >= 1f)
                {
                    sheenT = -1f;
                    // The stations carry their palette in the mesh UVs, so clearing the block restores the material.
                    for (int i = 0; i < shells.Length; i++) if (shells[i] != null) shells[i].SetPropertyBlock(null);
                }
            }
        }

        /// <summary>1 -> over-squash -> tiny rebound -> rest, each leg eased so the hit snaps and the settle is soft.</summary>
        static Vector3 Stretch(float u)
        {
            if (u < 0.42f) return Vector3.Lerp(Vector3.one, new Vector3(1.15f, 0.85f, 1.15f), Smooth(u / 0.42f));
            if (u < 0.75f)
                return Vector3.Lerp(new Vector3(1.15f, 0.85f, 1.15f), new Vector3(0.95f, 1.08f, 0.95f),
                    Smooth((u - 0.42f) / 0.33f));
            return Vector3.Lerp(new Vector3(0.95f, 1.08f, 0.95f), Vector3.one, Smooth((u - 0.75f) / 0.25f));
        }

        static float Smooth(float t) => t * t * (3f - 2f * t);

        void Burst()
        {
            if (sparks == null)
            {
                var tex = BobAmbientFx.Resource("fx_sparkle") ?? BobAmbientFx.Dot;
                sparks = BobAmbientFx.New("CleanSparkle", transform, tex);
                var main = sparks.main;
                main.loop = false;
                main.prewarm = false;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.8f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(1.3f, 2.5f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.09f, 0.18f);
                main.startColor = new Color(1f, 0.88f, 0.46f, 1f);
                main.gravityModifier = 0.5f;
                main.maxParticles = 60;

                var emission = sparks.emission;   // fully manual: one burst per completion
                emission.enabled = false;
                var shape = sparks.shape;
                shape.enabled = false;

                var colour = sparks.colorOverLifetime;
                colour.enabled = true;
                colour.color = BobAmbientFx.AlphaFade(Color.white, 1f);

                var size = sparks.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.3f));

                sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            sparks.transform.position = transform.position + Vector3.up * 1.05f;
            sparks.Clear();
            sparks.Play();
            sparks.Emit(sparkCount);
        }
    }
}
