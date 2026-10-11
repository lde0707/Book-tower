using UnityEngine;
using BookTower.UI;

namespace BookTower.Puzzles
{
    // Presentation may finish while the room is hidden. Physics and undo never own these transforms.
    public sealed partial class GravityPagePuzzle
    {
        float leafProgress, pageProgress;
        Vector3 leafStart, pageStart;
        Quaternion pageRotation;
        Collider[] exitBlockers;
        bool exitUnblocked;

        void InitializePresentation()
        {
            if (exitLeaf)
            {
                leafStart = exitLeaf.localPosition;
                exitBlockers = exitLeaf.GetComponentsInChildren<Collider>(true);
            }
            if (completedPage)
            {
                pageStart = completedPage.localPosition;
                pageRotation = completedPage.localRotation;
            }
        }

        void UpdateSolvedPresentation()
        {
            if (completedPage && pageProgress < 1)
            {
                pageProgress = PaperSettingsRuntime.Values.reduceStoryMotion ? 1 :
                    Mathf.MoveTowards(pageProgress, 1, Time.deltaTime / .7f);
                var turn = Quaternion.Euler(0, 90 * Mathf.SmoothStep(0, 1, pageProgress), 0);
                completedPage.SetLocalPositionAndRotation(
                    completedPageHinge + turn * (pageStart - completedPageHinge), turn * pageRotation);
            }
            if (!exitLeaf || leafProgress >= 1) return;
            leafProgress = Mathf.MoveTowards(leafProgress, 1, Time.deltaTime / 3f);
            exitLeaf.localPosition = leafStart + Vector3.up * (7f * Mathf.SmoothStep(0, 1, leafProgress));
            if (!exitUnblocked && leafProgress > .98f)
            {
                foreach (var blocker in exitBlockers) if (blocker) blocker.enabled = false;
                exitUnblocked = true;
            }
        }
    }
}
