using System.Collections;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public NPCDialogue npc;
    public UIHighlightSystem highlight;

    public RectTransform woodIcon;
    public RectTransform stoneIcon;
    public RectTransform foodIcon;

    public Transform stoneMineWorld;
    public Transform enemyCampWorld;

    public int worker = 0;
    public int wood = 0;
    [Header("TOP BAR ICONS")]
    public RectTransform goldIcon;   // icon vàng thưởng
    public RectTransform dayIcon;    // icon DAY counter

    void Start()
    {
        StartCoroutine(Intro());
    }

    IEnumerator Intro()
    {

        // ================= GOLD ICON =================
        highlight.Highlight(goldIcon);

        yield return npc.ShowAndWait(
            "Đây là vàng thưởng - nhận được khi đánh thắng kẻ địch."
        );

        // ================= DAY ICON =================
        highlight.Highlight(dayIcon);

        yield return npc.ShowAndWait(
            "Đây là DAY - thể hiện số ngày bạn đã sinh tồn."
        );

        highlight.ClearAll();
        // ================= WOOD =================
        UIHighlightSystem.Instance.Highlight(woodIcon);
        yield return npc.ShowAndWait("Đây là GỖ - dùng để xây dựng.");

        // ================= STONE =================
        UIHighlightSystem.Instance.Highlight(stoneIcon);
        yield return npc.ShowAndWait("Đây là ĐÁ - dùng để xây công trình.");

        // ================= FOOD =================
        UIHighlightSystem.Instance.Highlight(foodIcon);
        yield return npc.ShowAndWait("Đây là LÚA - nuôi dân làng.");

        UIHighlightSystem.Instance.ClearAll();

        yield return npc.ShowAndWait("Bắt đầu xây dựng làng!");







        // ================= MỎ ĐÁ =================
        yield return npc.ShowAndWait("Phía xa là mỏ đá.");

        highlight.ClearAll();

        npc.Show("Lia camera đến mỏ đá...");

        Camera.main.transform.position = stoneMineWorld.position;

        yield return npc.ShowAndWait("Mỏ đá đang bị khóa.");

        yield return npc.ShowAndWait("Cần 7 worker và 12 gỗ để mở.");

        // CHECK CONDITION
        yield return new WaitUntil(() => worker >= 7 && wood >= 12);

        yield return npc.ShowAndWait("Mỏ đá đã mở!");

        // ================= LỀU ĐỊCH =================
        yield return npc.ShowAndWait("Phía xa là lều địch.");

        highlight.ClearAll();

        Camera.main.transform.position = enemyCampWorld.position;

        yield return npc.ShowAndWait("Đây là nơi địch đóng quân.");

        yield return npc.ShowAndWait("Hãy chuẩn bị phòng thủ!");

        highlight.ClearAll();




    }
}