namespace Mufutu.Mobile.Core;

public static class FieldCopy
{
    public const string HubTitle = "Campo";
    public const string HubSubtitle = "Escolha o que quer fazer";
    public const string MyWork = "Minhas OTs";
    public const string ReportFault = "Reportar avaria";
    public const string Checklist = "Checklist";
    public const string Sync = "Enviar dados";
    public const string Start = "Começar";
    public const string Finish = "Terminei";
    public const string Send = "Enviar";
    public const string Back = "Voltar";
    public const string PhotoRequired = "Tire uma foto da avaria";
    public const string ChooseReason = "O que aconteceu?";
    public const string Sent = "Enviado";
    public const string Offline = "Sem rede";
    public const string Online = "Com rede";
    public const string NoWork = "Sem trabalhos";
    public const string LoginTitle = "MUFUTU Campo";
    public const string LoginSubtitle = "Técnicos · mineração Angola";

    public static readonly IReadOnlyDictionary<string, string> WoStatus = new Dictionary<string, string>
    {
        ["approved"] = "Para fazer",
        ["in_progress"] = "A fazer",
        ["completed"] = "Feito",
        ["pending_approval"] = "Aguarda",
        ["draft"] = "Rascunho",
        ["cancelled"] = "Cancelado",
    };

    public static readonly IReadOnlyList<FaultReason> FaultReasons =
    [
        new("oil_leak", "Fuga de óleo", "high"),
        new("overheat", "Motor aquece", "critical"),
        new("flat_tire", "Pneu furado", "high"),
        new("noise", "Ruído anormal", "medium"),
        new("hydraulic", "Hidráulica fraca", "high"),
        new("electrical", "Falha eléctrica", "critical"),
    ];

    public static readonly IReadOnlyList<ChecklistTemplate> ChecklistTemplates =
    [
        new("dozer", "Bulldozer", ["Óleo motor", "Água radiador", "Fugas no solo", "Rastos tensos"]),
        new("excavator", "Escavadora", ["Óleo hidráulico", "Pinos e buchas", "Cabine limpa", "Pneus / rodas"]),
        new("truck", "Camião", ["Pneus OTR", "Freios", "Luzes", "Nível diesel"]),
        new("generator", "Gerador", ["Nível diesel", "Bateria", "Ruído motor", "Tensão saída"]),
    ];
}

public sealed record FaultReason(string Id, string Label, string Priority);

public sealed record ChecklistTemplate(string Id, string Label, string[] Items);
