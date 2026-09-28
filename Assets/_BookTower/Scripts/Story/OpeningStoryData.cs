namespace BookTower.Story
{
    public enum StoryShot { Knock, ShareLight, Wake, FailedFlight, Promise, DoorMemory, CompareWing, NameMemory }

    public readonly struct StoryBeat
    {
        public readonly string speaker, text;
        public readonly StoryShot shot;
        public StoryBeat(StoryShot shot, string speaker, string text)
        { this.shot = shot; this.speaker = speaker; this.text = text; }
    }

    // Sequence IDs, rather than localized sentences, own progression.
    public static class OpeningStoryData
    {
        public const string Promise = "F0.Entry.Promise", Return = "F0.Complete.Memory01";
        public const string Theatre = "1A.Entry.Shadow", Name = "1A.Complete.Memory02";
        public static StoryBeat[] Beats(string id) => id switch
        {
            Promise => new[] {
                new StoryBeat(StoryShot.Knock, "종이새", "저기… 들려?"),
                new StoryBeat(StoryShot.Knock, "종이새", "빛이 거의 꺼졌네. 잠깐만… 내가 도와줄게."),
                new StoryBeat(StoryShot.ShareLight, "종이새", "내 빛을 조금 나눠 줄게.\n눈을 뜰 수 있으면 좋겠다."),
                new StoryBeat(StoryShot.Wake, "나", "따뜻해… 고마워. 여긴 어디지?"),
                new StoryBeat(StoryShot.Wake, "종이새", "책 속의 작업방이야. 길이 접히면서 갇혔어.\n마지막 장에 내 집이 있는데… 돌아갈 수가 없어."),
                new StoryBeat(StoryShot.FailedFlight, "나", "나는 아무것도 기억이 안 나.\n그런데 네 날개… 나한테 빛을 줘서 흐려진 거야?"),
                new StoryBeat(StoryShot.Promise, "종이새", "조금 쉬면 괜찮아. 저 작업책에 길을 잇는 그림이 있어.\n함께 살펴봐 줄래?"),
                new StoryBeat(StoryShot.Promise, "나", "응. 이번엔 내가 도울게. 네 집까지 같이 가자."),
            },
            Return => new[] {
                new StoryBeat(StoryShot.DoorMemory, "제본사의 기억", "잠깐 다녀와도 괜찮아.\n돌아올 자리는 남겨 둘게."),
                new StoryBeat(StoryShot.DoorMemory, "종이새", "이 목소리… 우리 집에서 들었어."),
            },
            Theatre => new[] {
                new StoryBeat(StoryShot.CompareWing, "종이새", "여기서는 다들 나보다 컸는데."),
                new StoryBeat(StoryShot.CompareWing, "나", "그림자가 컸던 걸까?"),
            },
            Name => new[] {
                new StoryBeat(StoryShot.NameMemory, "종이새", "나… 모아였어."),
                new StoryBeat(StoryShot.NameMemory, "나", "이제 부를 수 있겠다. 모아."),
            },
            _ => System.Array.Empty<StoryBeat>()
        };
    }
}
