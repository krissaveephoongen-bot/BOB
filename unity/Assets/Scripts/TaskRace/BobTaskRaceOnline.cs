// Battle of the Brooms: what an online Task Race adds on top of the offline one (docs 27 section 10).
//
// The room (Bob.Ugs.BobOnlineRoom) carries Netcode named messages; this component turns them into the things a player
// can see: the other players' heroes walking on the same island, and the shared station states. The task list itself is
// identical on every device because MatchRequest carries the host's seed (docs 24 section 15.23).
//
// Offline play never constructs this component, so none of it can leak into solo/story.
using System.Collections.Generic;
using UnityEngine;

namespace Bob.TaskRace
{
    public sealed class BobTaskRaceOnline : MonoBehaviour
    {
        const float PoseHz = 10f;
        const float PollHz = 4f;
        const float FollowRate = 12f;

        sealed class Remote
        {
            public Transform body;
            public Vector3 target;
            public float yaw;
        }

        Bob.Ugs.BobOnlineRoom room;
        BobTaskRaceHero hero;
        TaskRaceRound round;
        TaskRaceModeManager manager;
        TeamId team;
        readonly Dictionary<int, Remote> remotes = new Dictionary<int, Remote>();
        readonly Dictionary<string, bool> knownDone = new Dictionary<string, bool>();
        readonly List<string> heroPool = new List<string>();
        float nextPose, nextPoll;
        int remoteCount;
        bool roomGone;

        /// <summary>Attach to the running match when the lobby handed over an online room (called by TaskRaceModeManager).</summary>
        public static BobTaskRaceOnline Attach()
        {
            var room = Bob.Ugs.BobOnlineRoom.Instance;
            if (room == null || !room.InRoom) return null;
            var go = new GameObject("TaskRaceOnline");
            return go.AddComponent<BobTaskRaceOnline>();
        }

        void Awake()
        {
            room = Bob.Ugs.BobOnlineRoom.Instance;
            hero = FindAnyObjectByType<BobTaskRaceHero>();
            round = FindAnyObjectByType<TaskRaceRound>();
            manager = TaskRaceModeManager.Instance;
            team = manager != null ? manager.localTeam : TeamId.TeamA;

            if (room != null)
            {
                room.PoseReceived += OnPose;
                room.TaskReceived += OnTask;
                room.SeriesResultApplied += OnSeriesResult;
                room.SeriesRoundRequested += OnSeriesRound;
                room.SeriesAbandoned += OnSeriesAbandoned;
                foreach (var id in room.Roster)
                    if (!string.IsNullOrEmpty(id) && !heroPool.Contains(id)) heroPool.Add(id);
            }

            // The stations that are already clean at the first frame are the baseline, not events to send.
            if (round != null)
                foreach (var spot in round.StationsOf(team))
                    knownDone[spot.SpotId] = spot.Done;

            Debug.Log($"[TaskRace] online · host {MatchHost()} · {room?.Players ?? 0} player(s) · roster {heroPool.Count}");
        }

        void OnDestroy()
        {
            if (room == null) return;
            room.PoseReceived -= OnPose;
            room.TaskReceived -= OnTask;
            room.SeriesResultApplied -= OnSeriesResult;
            room.SeriesRoundRequested -= OnSeriesRound;
            room.SeriesAbandoned -= OnSeriesAbandoned;
        }

        void Update()
        {
            if (room == null) return;

            // The host closed the room (or its connection died) while a series was in play: the shared series cannot
            // continue, so it is voided here too (docs 27 section 10.7). A decided series is left alone.
            if (!roomGone && !room.InRoom)
            {
                roomGone = true;
                if (manager != null && manager.Series != null && !manager.Series.Over) manager.OnOnlineSeriesAbandoned();
            }

            if (hero != null && Time.time >= nextPose)
            {
                nextPose = Time.time + 1f / PoseHz;
                room.SendPose(hero.transform.position, hero.transform.eulerAngles.y);
            }

            float k = 1f - Mathf.Exp(-FollowRate * Time.deltaTime);
            foreach (var remote in remotes.Values)
            {
                if (remote.body == null) continue;
                remote.body.position = Vector3.Lerp(remote.body.position, remote.target, k);
                remote.body.rotation = Quaternion.Slerp(remote.body.rotation, Quaternion.Euler(0f, remote.yaw, 0f), k);
            }

            if (round != null && Time.time >= nextPoll)
            {
                nextPoll = Time.time + 1f / PollHz;
                PollCompleted();
            }
        }

        /// <summary>Stations completed on this device since the last poll become one event each.</summary>
        void PollCompleted()
        {
            foreach (var spot in round.StationsOf(team))
            {
                string id = spot.SpotId;
                bool done = spot.Done;
                if (knownDone.TryGetValue(id, out bool was) && was == done) continue;
                knownDone[id] = done;
                if (done) room.SendTask(id, true);
            }
        }

        void OnPose(int netId, Vector3 position, float yaw)
        {
            if (room != null && netId == room.LocalNetId) return;
            var remote = RemoteFor(netId);
            if (remote == null) return;
            remote.target = new Vector3(position.x, remote.target.y, position.z);
            remote.yaw = yaw;
        }

        void OnTask(int netId, int roundNumber, string spotId, bool completed)
        {
            if (room != null && netId == room.LocalNetId) return;
            if (!completed || round == null || string.IsNullOrEmpty(spotId)) return;
            // A packet from the round before this one must not clean a freshly drawn station (docs 27 section 10.7).
            int current = manager != null && manager.Series != null ? manager.Series.Round : 1;
            if (roundNumber != current) return;
            foreach (var spot in round.StationsOf(team))
            {
                if (spot.SpotId != spotId) continue;
                spot.SettleFromRoom();
                // Only a station that really ended clean is marked known; a rejected update stays unsynced and the next
                // poll sends this device's own state instead of silently losing the completion.
                if (spot.Done) knownDone[spotId] = true;
                return;
            }
        }

        // ---------------------------------------------------------------- best-of-three (docs 27 section 10.7)

        void OnSeriesResult(SeriesRound line)
        {
            if (manager != null) manager.OnOnlineSeriesRound(line);
        }

        void OnSeriesRound(int roundNumber, int seed, float seconds)
        {
            if (manager != null) manager.BeginOnlineRound(roundNumber, seed, seconds);
        }

        void OnSeriesAbandoned()
        {
            if (manager != null) manager.OnOnlineSeriesAbandoned();
        }

        /// <summary>A body for a client we have not seen yet; the next unused roster hero wears it.</summary>
        Remote RemoteFor(int netId)
        {
            if (remotes.TryGetValue(netId, out var found)) return found;
            string heroId = heroPool.Count > 1 ? heroPool[(remoteCount + 1) % heroPool.Count] : "sweep";
            remoteCount++;
            var go = BobTaskRaceTeammates.Avatar(heroId, "Remote" + netId);
            var at = hero != null ? hero.transform.position + new Vector3(-1.4f * remoteCount, 0f, 1.4f) : Vector3.zero;
            go.transform.position = at;
            var remote = new Remote { body = go.transform, target = at, yaw = 0f };
            remotes[netId] = remote;
            Debug.Log($"[TaskRace] online · remote hero {heroId} for client {netId}");
            return remote;
        }

        static bool MatchHost() => Bob.Game.MatchRequest.Has && Bob.Game.MatchRequest.Host;
    }
}
