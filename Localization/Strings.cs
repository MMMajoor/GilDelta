namespace GilDelta.Localization;

public static class Strings
{
    private static Language _current = Language.English;

    public static void SetLanguage(Language lang) => _current = lang;

    private static string T(string en, string ja, string de, string fr, string zh, string ko) =>
        _current switch
        {
            Language.English           => en,
            Language.Japanese          => ja,
            Language.German            => de,
            Language.French            => fr,
            Language.SimplifiedChinese => zh,
            Language.Korean            => ko,
            _                          => en,
        };

    // ─── Generic ───
    public static string PluginName => "GilDelta";

    // ─── Widget ───
    public static string TotalWealth => T(
        "Total wealth",
        "総資産",
        "Gesamtvermögen",
        "Patrimoine total",
        "总资产",
        "총자산"
    );

    public static string Today => T("today", "今日", "heute", "aujourd'hui", "今天", "오늘");

    public static string LastUpdate => T(
        "last update",
        "最終更新",
        "letzte Aktualisierung",
        "dernière maj",
        "最近更新",
        "최근 업데이트"
    );

    // ─── Dashboard tabs ───
    public static string TabTimeline  => T("Timeline", "タイムライン", "Zeitachse", "Chronologie", "时间线", "타임라인");
    public static string TabChart     => T("Chart",    "チャート",     "Diagramm",  "Graphique",   "图表",    "차트");
    public static string TabBreakdown => T("Breakdown","内訳",         "Aufschlüsselung","Répartition","明细", "내역");
    public static string TabHeatmap   => T("Heatmap",  "ヒートマップ", "Heatmap",   "Carte de chaleur","热力图","히트맵");

    // ─── Categories ───
    public static string CategorySubmarineReturn => T(
        "Submarine return", "潜水艦帰還", "Rückkehr U-Boot", "Retour sous-marin", "潜艇返回", "잠수함 귀환"
    );
    public static string CategoryRetainerSale => T(
        "Retainer sale", "リテイナー売上", "Gehilfenverkauf", "Vente de servant", "雇员销售", "고용인 판매"
    );
    public static string CategoryRetainerWithdraw => T(
        "Retainer withdraw", "リテイナー引出", "Abhebung", "Retrait servant", "雇员取款", "고용인 인출"
    );
    public static string CategoryRetainerDeposit => T(
        "Retainer deposit", "リテイナー預入", "Einzahlung", "Dépôt servant", "雇员存款", "고용인 입금"
    );
    public static string CategoryNpcShopBuy => T(
        "NPC purchase", "NPC購入", "NSC-Kauf", "Achat NPC", "NPC购买", "NPC 구매"
    );
    public static string CategoryNpcShopSell => T(
        "NPC sale", "NPC売却", "NSC-Verkauf", "Vente NPC", "NPC出售", "NPC 판매"
    );
    public static string CategoryMarketBoardBuy => T(
        "Marketboard purchase", "マケボ購入", "Marktbrett-Kauf", "Achat marché", "市场购买", "마켓보드 구매"
    );
    public static string CategoryRepair => T(
        "Repair", "修理", "Reparatur", "Réparation", "修理", "수리"
    );
    public static string CategoryTeleport => T(
        "Teleport", "テレポ", "Teleport", "Téléportation", "传送", "텔레포트"
    );
    public static string CategoryMisc => T(
        "Other", "その他", "Sonstiges", "Autre", "其他", "기타"
    );
}
