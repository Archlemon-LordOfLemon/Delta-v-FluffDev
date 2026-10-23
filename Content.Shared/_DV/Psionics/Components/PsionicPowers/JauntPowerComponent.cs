using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._DV.Psionics.Components.PsionicPowers;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DisplacementPowerComponent : BasePsionicPowerComponent
{
    public override EntProtoId ActionProtoId { get; set; } = "ActionDisplacement";

    public override string PowerName { get; set; } = "psionic-power-name-Displacement";

    public override int MinGlimmerChanged { get; set; } = 5;

    public override int MaxGlimmerChanged { get; set; } = 15;
}
