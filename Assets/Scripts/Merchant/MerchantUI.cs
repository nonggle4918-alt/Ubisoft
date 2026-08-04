using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Fixed 6-slot layout (MerchantManager always generates exactly 4 piece + 2 relic
// offers), so slots are wired once in the scene rather than instantiated per offer.
public class MerchantUI : MonoBehaviour
{
    public GameObject shopPanel;
    public Button[] slotButtons;
    public TextMeshProUGUI[] slotNameTexts;
    public TextMeshProUGUI[] slotPriceTexts;
    public Button startButton;

    private void Start()
    {
        if (shopPanel != null)
            shopPanel.SetActive(false);

        if (startButton != null)
        {
            startButton.onClick.AddListener(() => MerchantManager.Instance?.RequestStart());
            SFXManager.Instance?.BindButtonClickSound(startButton);
        }

        StartCoroutine(SubscribeWhenReady());
    }

    // MerchantManager is created at runtime by UIManager (EnsureMerchantManager), and
    // Start() order between scripts already in the scene vs. runtime-created ones isn't
    // guaranteed — subscribing only once Instance actually exists avoids missing it forever.
    private IEnumerator SubscribeWhenReady()
    {
        while (MerchantManager.Instance == null)
            yield return null;

        MerchantManager.Instance.OnOffersGenerated += RebuildSlots;
        MerchantManager.Instance.OnOfferPurchased += _ => RebuildSlots();
        MerchantManager.Instance.OnShopClosed += HandleShopClosed;
    }

    private void OnDestroy()
    {
        if (MerchantManager.Instance == null) return;
        MerchantManager.Instance.OnOffersGenerated -= RebuildSlots;
        MerchantManager.Instance.OnShopClosed -= HandleShopClosed;
    }

    private void HandleShopClosed()
    {
        if (shopPanel != null)
            shopPanel.SetActive(false);
    }

    private void RebuildSlots()
    {
        if (shopPanel != null)
            shopPanel.SetActive(true);
        if (MerchantManager.Instance == null) return;

        var offers = MerchantManager.Instance.CurrentOffers;
        for (int i = 0; i < slotButtons.Length; i++)
        {
            if (slotButtons[i] == null) continue;

            if (i >= offers.Count)
            {
                slotButtons[i].gameObject.SetActive(false);
                continue;
            }

            MerchantOffer offer = offers[i];
            slotButtons[i].gameObject.SetActive(true);
            slotButtons[i].interactable = !offer.sold;

            if (i < slotNameTexts.Length && slotNameTexts[i] != null)
                slotNameTexts[i].text = BuildName(offer);
            if (i < slotPriceTexts.Length && slotPriceTexts[i] != null)
                slotPriceTexts[i].text = offer.sold ? "판매완료" : $"{offer.price}G";

            slotButtons[i].onClick.RemoveAllListeners();
            slotButtons[i].onClick.AddListener(() => MerchantManager.Instance.TryPurchase(offer));
        }
    }

    private static string BuildName(MerchantOffer offer)
    {
        if (offer.category == MerchantOfferCategory.Relic)
            return offer.relic != null ? offer.relic.displayName : "?";

        string tierSuffix = offer.category == MerchantOfferCategory.Normal ? $" T{offer.tier}" : "";
        return offer.DisplayName + tierSuffix;
    }
}
