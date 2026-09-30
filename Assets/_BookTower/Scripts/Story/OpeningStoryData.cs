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
                new StoryBeat(StoryShot.DoorMemory, "나", "문틈에서… 누가 말하는 소리가 나."),
                new StoryBeat(StoryShot.DoorMemory, "제본사의 기억", "잠깐 다녀와도 괜찮아.\n돌아올 자리는 남겨 둘게."),
                new StoryBeat(StoryShot.DoorMemory, "종이새", "나한테 하던 말이야. 집을 나설 때마다…\n이 목소리는 잊지 않았네."),
                new StoryBeat(StoryShot.DoorMemory, "나", "이 책에 네 기억이 남아 있나 봐.\n문 너머에도 있을까? 같이 가 보자."),
            },
            Theatre => new[] {
                new StoryBeat(StoryShot.CompareWing, "종이새", "잠깐… 이 그림들, 본 적 있어.\n집에 가는 길에 들르던 작은 극장이야."),
                new StoryBeat(StoryShot.CompareWing, "나", "토끼도 있고, 여우도 있네.\n그런데 이야기가 중간에서 끊긴 것 같아."),
                new StoryBeat(StoryShot.CompareWing, "종이새", "등불이 켜지면 저 벽에 친구들이 나타났어.\n나는 앞에 앉아서 보고… 누가 날 불러 줬는데."),
                new StoryBeat(StoryShot.CompareWing, "나", "그때 장면을 다시 볼 수 있으면 기억날지도 몰라.\n여기 남은 것들을 같이 살펴보자."),
            },
            Name => new[] {
                new StoryBeat(StoryShot.NameMemory, "종이새", "맞아… 토끼 다음에 여우가 나왔어.\n둘이 집으로 돌아가는 이야기였지."),
                new StoryBeat(StoryShot.NameMemory, "종이새", "끝나면 그 목소리가 말했어.\n‘모아, 이제 우리도 집에 가자.’"),
                new StoryBeat(StoryShot.NameMemory, "나", "모아… 그게 네 이름이구나."),
                new StoryBeat(StoryShot.NameMemory, "종이새", "응. 누군가 날 기다렸다는 것도 생각났어."),
                new StoryBeat(StoryShot.NameMemory, "나", "그럼 그곳까지 같이 가자, 모아.\n이번엔 이름도 잊지 않게 내가 불러 줄게."),
            },
            _ => System.Array.Empty<StoryBeat>()
        };
    }
}
