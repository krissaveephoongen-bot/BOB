// Battle of the Brooms: the story strings that live outside the six ChapterDefinition assets, in one place that the
// offline checks can read. BobChapters re-exports every one of them, so the game code keeps saying "BobChapters" while
// the strings stay testable without Unity (the chapter assets themselves need Resources.Load).
namespace Bob.Rules
{
    public static class BobStoryText
    {
        /// <summary>
        /// The tagline (docs 29 section 6 item 7, settled by the owner on 16 Nov 2026): the shortest true explanation
        /// of best-of-three. Kept in Thai only on purpose — the font set carries no emoji and no star glyph.
        /// </summary>
        public const string Slogan = "งานสะอาดต้องสามยก";

        /// <summary>End-of-story card, shown once chapter 6 is passed (docs 29 section 5).</summary>
        public const string FinaleTitle = "ตลาดท่าศาลาเปิดแล้ว";
        public const string FinaleLine = "ย่านหนึ่งจะไม่ตาย ตราบใดที่ยังมีคนกวาดมัน";
        public const string FinaleNote = "สไลม์จิ๋วมงกุฎเอียงกำลังนอนอยู่ในโหลที่ HQ";
    }
}
