public enum MerchantOfferCategory { Normal, Special, Hero, Relic }

public class MerchantOffer
{
    public MerchantOfferCategory category;
    public PieceData templateData;   // shared project asset for Normal/Special; runtime instance for Hero
    public int tier;                 // only meaningful for Normal
    public RelicData relic;          // only set for Relic
    public int price;
    public bool sold;

    public string DisplayName =>
        category == MerchantOfferCategory.Relic
            ? (relic != null ? relic.displayName : "?")
            : (templateData != null ? templateData.pieceName : "?");
}
