namespace BookTower.UI
{
 // Only witnessed story milestones belong in the journal, never puzzle solutions.
 public static class StoryJournal
 {
  public static bool TryGet(string id,out string title,out string text)
  {
   title="";text="";
   switch(id)
   {
    case "Story.Promise":title="함께 돌아갈 곳";text="종이새가 나눠 준 빛으로 깨어났다. 이전 일은 기억나지 않는다.\n돌아갈 길을 잃은 새와 마지막 장의 집을 함께 찾기로 했다.";break;
    case "M01":title="돌아올 자리";text="문 너머에서 새를 기다리던 목소리를 들었다.\n이 책에는 새가 잊어버린 기억이 남아 있는 듯하다.";break;
    case "M02":title="새의 이름, 모아";text="작은 극장의 장면을 보고 새가 이름을 기억해 냈다. 모아.\n공연이 끝나면 함께 집으로 가던 누군가가 있었다.";break;
    case "M03":title="닫힌 문";text="소중한 것을 지키려고 문을 닫았지만, 안에는 어둠만 남았다.\n다시 빛을 들이는 일이 필요했다.";break;
    default:return false;
   }
   return true;
  }
 }
}
