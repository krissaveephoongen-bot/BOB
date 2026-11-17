// Battle of the Brooms: asking the server which day it is and what seed it carries (docs 31 section 2.2).
//
// The endpoint answers one small JSON object; nothing here is trusted blindly. A reply is used only when its seed is the
// seed its own day number must produce (BobDaily.Matches), so a truncated, stale or hand-written answer cannot start a
// private task list. When the answer is refused or the server is unreachable the card says so and the ordinary modes
// still work — the daily is the only thing that needs a network.
//
// The URL lives in PlayerPrefs ("bob.daily.url") because the address differs per build: the default below is the local
// dev server (node server/daily-server.js), reachable on a desktop build and on an emulator that shares the host.
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Bob.Game
{
    /// <summary>One answer from GET /daily/seed, plus what the card needs to explain a refusal.</summary>
    [Serializable]
    public sealed class BobDailyReply
    {
        public bool ok;
        public string day = "";
        public int dayNumber;
        public int seed;
        public int rounds;
        public int nextDayInSec;
        /// <summary>Set by the client, never by the server: "" when the reply is usable, else the reason it is not.</summary>
        [NonSerialized] public string error;

        public bool Valid => ok && error == null && dayNumber > 0 && seed > 0 && rounds > 0 &&
                             Bob.Rules.BobDaily.Matches(seed, dayNumber);
    }

    public static class BobDailyFeed
    {
        public const string UrlKey = "bob.daily.url";
        /// <summary>The dev server of server/daily-server.js; a real build sets bob.daily.url to the deployed address.</summary>
        public const string DefaultUrl = "http://127.0.0.1:8099/daily/seed";
        public const int TimeoutSeconds = 8;

        public static string Url
        {
            get
            {
                try
                {
                    var u = PlayerPrefs.GetString(UrlKey, "");
                    return string.IsNullOrEmpty(u) ? DefaultUrl : u;
                }
                catch (Exception) { return DefaultUrl; }
            }
        }

        /// <summary>
        /// One request, one callback. Always calls back exactly once: an unreachable server answers with error
        /// "offline", a wrong-shaped reply with "bad reply", a refused day with "http 409".
        /// </summary>
        public static IEnumerator Fetch(Action<BobDailyReply> done)
        {
            var reply = new BobDailyReply();
            using (var req = UnityWebRequest.Get(Url))
            {
                req.timeout = TimeoutSeconds;
                req.downloadHandler = new DownloadHandlerBuffer();
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    reply.error = req.responseCode > 0 ? "http " + req.responseCode : "offline";
                    if (Application.isPlaying) Debug.Log("[Daily] " + Url + " -> " + reply.error);
                    done?.Invoke(reply);
                    yield break;
                }

                var body = req.downloadHandler != null ? req.downloadHandler.text : "";
                BobDailyReply parsed = null;
                try { parsed = JsonUtility.FromJson<BobDailyReply>(body); } catch (Exception) { parsed = null; }
                if (parsed == null)
                {
                    reply.error = "bad reply";
                }
                else
                {
                    reply = parsed;
                    reply.error = reply.Valid ? null : "bad seed";
                }
                if (Application.isPlaying)
                    Debug.Log("[Daily] " + (reply.error ?? ("day " + reply.dayNumber + " seed " + reply.seed)));
                done?.Invoke(reply);
            }
        }
    }
}
