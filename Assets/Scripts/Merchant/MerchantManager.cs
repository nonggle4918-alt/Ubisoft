using System;
using System.Collections.Generic;
using UnityEngine;

// Offer/price/purchase logic only — MerchantUI owns rendering, TimerManager owns when the
// shop is allowed to appear (see GameManager.IsMerchantWave).
public class MerchantManager : MonoBehaviour
{
    public static MerchantManager Instance { get; private set; }

    [SerializeField] private float priceScale = 1f;
    [SerializeField] private int heroBasePrice = 120;
    [SerializeField] private int relicPriceFallback = 100;

    private const int FixedNormalSlots = 2;
    private const int RandomCategorySlots = 2;
    private const int RelicSlots = 2;

    private static readonly string[] NormalNames = { "bishop", "knight", "rook" };
    private static readonly string[] SpecialNames = { "pawn", "queen", "king" };

    public bool IsShopOpen { get; private set; }
    public IReadOnlyList<MerchantOffer> CurrentOffers => currentOffers;
    public event Action OnOffersGenerated;
    public event Action<MerchantOffer> OnOfferPurchased;
    public event Action OnShopClosed;

    private readonly List<MerchantOffer> currentOffers = new List<MerchantOffer>();
    private PieceManager pieceManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        pieceManager = FindFirstObjectByType<PieceManager>();
    }

    public void OpenShop()
    {
        GenerateOffers();
        IsShopOpen = true;
        OnOffersGenerated?.Invoke();
    }

    // Wired to the shop panel's "Start" button.
    public void RequestStart() => CloseShop();

    public void CloseShop()
    {
        if (!IsShopOpen) return;

        IsShopOpen = false;
        // Hero offers are freshly-created runtime instances (PromotionFactory.CreateByName);
        // unlike Normal/Special offers they aren't a shared project asset, so an unsold one
        // must be destroyed here or it leaks for the rest of the session.
        foreach (MerchantOffer offer in currentOffers)
        {
            if (!offer.sold && offer.category == MerchantOfferCategory.Hero && offer.templateData != null)
                Destroy(offer.templateData);
        }
        currentOffers.Clear();
        OnShopClosed?.Invoke();
    }

    private void GenerateOffers()
    {
        currentOffers.Clear();
        if (pieceManager == null)
            pieceManager = FindFirstObjectByType<PieceManager>();

        for (int i = 0; i < FixedNormalSlots; i++)
            currentOffers.Add(BuildNormalOffer());

        for (int i = 0; i < RandomCategorySlots; i++)
            currentOffers.Add(BuildRandomCategoryOffer());

        if (RelicManager.Instance != null)
        {
            foreach (RelicData relic in RelicManager.Instance.GetRandomRelics(RelicSlots))
            {
                currentOffers.Add(new MerchantOffer
                {
                    category = MerchantOfferCategory.Relic,
                    relic = relic,
                    price = relic.price > 0 ? relic.price : relicPriceFallback
                });
            }
        }
    }

    // Category weights Normal:Special:Hero = 2:1:1 (sums to 4).
    private MerchantOffer BuildRandomCategoryOffer()
    {
        int roll = UnityEngine.Random.Range(0, 4);
        if (roll < 2) return BuildNormalOffer();
        if (roll < 3) return BuildSpecialOffer();
        return BuildHeroOffer();
    }

    private MerchantOffer BuildNormalOffer()
    {
        string name = NormalNames[UnityEngine.Random.Range(0, NormalNames.Length)];
        PieceData template = pieceManager != null ? pieceManager.GetTemplateByName(name) : null;
        int tier = RollMerchantTier();
        return new MerchantOffer
        {
            category = MerchantOfferCategory.Normal,
            templateData = template,
            tier = tier,
            price = ComputeNormalPrice(tier)
        };
    }

    private MerchantOffer BuildSpecialOffer()
    {
        string name = SpecialNames[UnityEngine.Random.Range(0, SpecialNames.Length)];
        PieceData template = pieceManager != null ? pieceManager.GetTemplateByName(name) : null;
        return new MerchantOffer
        {
            category = MerchantOfferCategory.Special,
            templateData = template,
            tier = 1,
            price = ComputeGachaWeightPrice(template)
        };
    }

    private MerchantOffer BuildHeroOffer()
    {
        string heroName = PromotionFactory.RollHeroName();
        PieceData runtimeHero = PromotionFactory.CreateByName(heroName, null);
        return new MerchantOffer
        {
            category = MerchantOfferCategory.Hero,
            templateData = runtimeHero,
            tier = 1,
            price = ComputeHeroPrice(heroName)
        };
    }

    // Rolls a tier 3-5 result by renormalizing the existing TierDraw weights to just that
    // range — keeps the "at least tier 3" guarantee without a separate probability table.
    private int RollMerchantTier()
    {
        GameDatabase database = GameManager.Instance?.Database;
        if (database == null) return 3;

        float total = 0f;
        foreach (var row in database.TierDraw.rows)
            if (row.tier >= 3) total += row.weight;

        if (total <= 0f) return 3;

        float roll = UnityEngine.Random.Range(0f, total);
        float cumulative = 0f;
        foreach (var row in database.TierDraw.rows)
        {
            if (row.tier < 3) continue;
            cumulative += row.weight;
            if (roll < cumulative) return row.tier;
        }
        return 5;
    }

    // price = baseCost * sqrt(totalWeight / itemWeight) * priceScale — the rarer the
    // roll actually is (full 1-5 table, not the renormalized 3-5 roll above), the higher
    // the price, softened by sqrt so tier 5 isn't absurdly expensive.
    private int ComputeNormalPrice(int tier)
    {
        GameDatabase database = GameManager.Instance?.Database;
        if (database == null || pieceManager == null) return heroBasePrice;

        float totalWeight = 0f;
        float itemWeight = 1f;
        foreach (var row in database.TierDraw.rows)
        {
            totalWeight += row.weight;
            if (row.tier == tier) itemWeight = row.weight;
        }
        itemWeight = Mathf.Max(0.01f, itemWeight);

        int baseCost = pieceManager.CurrentPullCost;
        return Mathf.RoundToInt(baseCost * Mathf.Sqrt(totalWeight / itemWeight) * priceScale);
    }

    private int ComputeGachaWeightPrice(PieceData template)
    {
        if (pieceManager == null || template == null || template.gachaWeight <= 0) return heroBasePrice;

        float totalWeight = pieceManager.TotalGachaWeight;
        float itemWeight = template.gachaWeight;
        int baseCost = pieceManager.CurrentPullCost;
        return Mathf.RoundToInt(baseCost * Mathf.Sqrt(totalWeight / itemWeight) * priceScale);
    }

    private int ComputeHeroPrice(string heroName)
    {
        int totalWeight = 0;
        int itemWeight = 18;
        foreach (var entry in PromotionFactory.HeroWeights)
        {
            totalWeight += entry.weight;
            if (entry.name == heroName) itemWeight = entry.weight;
        }
        itemWeight = Mathf.Max(1, itemWeight);

        return Mathf.RoundToInt(heroBasePrice * Mathf.Sqrt((float)totalWeight / itemWeight) * priceScale);
    }

    public bool TryPurchase(MerchantOffer offer)
    {
        if (offer == null || offer.sold || GameManager.Instance == null) return false;
        if (!GameManager.Instance.SpendGold(offer.price)) return false;

        bool success;
        if (offer.category == MerchantOfferCategory.Relic)
        {
            success = RelicManager.Instance != null && RelicManager.Instance.TryPurchase(offer.relic);
        }
        else if (pieceManager == null || offer.templateData == null)
        {
            success = false;
        }
        else
        {
            PieceData runtimeData = offer.category == MerchantOfferCategory.Hero
                ? offer.templateData
                : pieceManager.BuildRuntimeData(offer.templateData, offer.tier);
            success = pieceManager.TryPlaceRuntimePiece(runtimeData, out _);
        }

        if (!success)
        {
            // Exact refund — must not go through AddGold's gold-gain relic multiplier.
            GameManager.Instance.RefundGold(offer.price);
            return false;
        }

        offer.sold = true;
        OnOfferPurchased?.Invoke(offer);
        return true;
    }
}
