// Battle of the Brooms: the offline teammates of a Duo/Team pick (docs 11 S14). Until the online lobby of docs 27 can
// seat real friends, the empty slots of your side are helper bots: they walk to a dirty station of the player's own
// island, clean it at a deliberately slower pace than the player, and never raid, so the shared task list still needs
// the player. Each one wears the real mesh of the hero whose slot it fills.
using UnityEngine;
using UnityEngine.Rendering;

namespace Bob.TaskRace
{
    public static class BobTaskRaceTeammates
    {
        /// <summary>The hero mesh prefab of each roster entry lives under Resources/Characters.</summary>
        public const string HeroResourcePath = "Characters/SK_hero_";

        /// <summary>
        /// Place one bot per hero id around the local hero and hand it the player's team. Returns how many were placed
        /// (0 when the scene has no hero or no ids, which is the direct-play/verify case).
        /// </summary>
        public static int Spawn(string[] heroIds, TeamId team)
        {
            if (heroIds == null || heroIds.Length == 0) return 0;
            var hero = Object.FindAnyObjectByType<BobTaskRaceHero>();
            if (hero == null) return 0;

            var host = new GameObject("Teammates").transform;
            int placed = 0;
            for (int i = 0; i < heroIds.Length; i++)
            {
                // Fan out behind the spawn point on the clear south field, the same area the hero starts on.
                float side = i % 2 == 0 ? 1f : -1f;
                var at = hero.transform.position + new Vector3(side * (1.3f + 0.5f * (i / 2)), 0f, 1.1f * (1 + i / 2));
                var go = new GameObject("Teammate" + (i + 1));
                go.transform.SetParent(host, false);
                go.transform.position = at;

                var bot = go.AddComponent<TaskRaceBotController>();
                bot.team = team;
                bot.raidEnemy = false;  // a teammate keeps to its own island
                bot.pace = 2.2f;        // slower than the rival's 1.12: the player is meant to clean the bigger share
                bot.headStart = 6f + i * 2f;
                bot.buyShield = true;

                Body(go.transform, heroIds[i]);
                placed++;
                Debug.Log($"[TaskRace] offline teammate {i + 1} \"{heroIds[i]}\" on {team} at ({at.x:0.0}, {at.z:0.0}).");
            }
            return placed;
        }

        /// <summary>
        /// One remote player's body (docs 27 online room): the same roster mesh as a teammate, with no brain attached —
        /// the online component moves it from the poses the other device sends.
        /// </summary>
        public static GameObject Avatar(string heroId, string name, Transform parent = null)
        {
            var go = new GameObject(string.IsNullOrEmpty(name) ? "Remote" : name);
            if (parent != null) go.transform.SetParent(parent, false);
            Body(go.transform, heroId);
            return go;
        }

        /// <summary>The roster mesh under Resources/Characters, or a capsule so a missing prefab still walks.</summary>
        static void Body(Transform parent, string heroId)
        {
            var prefab = string.IsNullOrEmpty(heroId) ? null : Resources.Load<GameObject>(HeroResourcePath + heroId);
            if (prefab == null) prefab = Resources.Load<GameObject>(HeroResourcePath + "sweep");
            if (prefab != null)
            {
                var body = Object.Instantiate(prefab, parent);
                body.name = prefab.name;
                body.transform.localPosition = Vector3.zero;
                // the prefab root carries the FBX importer's upright rotation; compose, never replace
                body.transform.localRotation = Quaternion.Euler(0f, 90f, 0f) * prefab.transform.localRotation;
                foreach (var r in body.GetComponentsInChildren<Renderer>(true))
                {
                    r.shadowCastingMode = ShadowCastingMode.On;
                    r.receiveShadows = true;
                }
                return;
            }
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Body";
            capsule.transform.SetParent(parent, false);
            capsule.transform.localScale = new Vector3(0.55f, 0.575f, 0.55f);
            capsule.transform.localPosition = new Vector3(0f, 0.575f, 0f);
            Object.Destroy(capsule.GetComponent<Collider>());
        }
    }
}
