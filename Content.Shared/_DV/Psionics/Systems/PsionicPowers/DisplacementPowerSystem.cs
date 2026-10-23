using Content.Shared._DV.Psionics.Components;
using Content.Shared._DV.Psionics.Components.PsionicPowers;
using Content.Shared._DV.Psionics.Events;
using Content.Shared._DV.Psionics.Events.PowerActionEvents;
using Content.Shared.Interaction;
using Content.Shared.Physics;
using Content.Shared.Popups;

namespace Content.Shared._DV.Psionics.Systems.PsionicPowers;

/// <summary>
/// Teleports the performer to the targeted location, then back to where they started after a short delay.
/// </summary>
public sealed class DisplacementPowerSystem : BasePsionicPowerSystem<DisplacementPowerComponent, DisplacementPowerActionEvent>
{
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisplacementActiveComponent, DispelledEvent>(OnDispelled);
    }

    protected override void OnPowerUsed(Entity<JauntPowerComponent> psionic, ref DisplacementPowerActionEvent args)
    {
        var performer = args.Performer;

        // Casting again mid-jaunt would overwrite the saved origin and strand them at the first destination.
        // Handled is set to false so a rejected cast doesn't put the action on cooldown.
        if (HasComp<PsionicJauntActiveComponent>(performer))
        {
            args.Handled = false;
            return;
        }

        var xform = Transform(performer);
        var origin = xform.Coordinates;

        if (xform.MapID != _transform.GetMapId(args.Target)
            || !_interaction.InRangeUnobstructed(performer,
                args.Target,
                range: psionic.Comp.MaxRange,
                collisionMask: CollisionGroup.Opaque,
                popup: true))
        {
            args.Handled = false;
            return;
        }

        var active = EnsureComp<PsionicJauntActiveComponent>(performer);
        active.Origin = origin;
        active.ReturnTime = Timing.CurTime + psionic.Comp.Duration;
        Dirty(performer, active);

        _transform.SetCoordinates(performer, args.Target);
        _transform.AttachToGridOrMap(performer, xform);

        AfterPowerUsed(psionic, performer);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PsionicJauntActiveComponent>();
        while (query.MoveNext(out var uid, out var active))
        {
            if (Timing.CurTime < active.ReturnTime)
                continue;

            ReturnFromJaunt((uid, active));
        }
    }

    /// <summary>
    /// Dispelling someone mid-jaunt snaps them back immediately.
    /// </summary>
    private void OnDispelled(Entity<PsionicJauntActiveComponent> jaunter, ref DispelledEvent args)
    {
        ReturnFromJaunt(jaunter);
    }

    private void ReturnFromJaunt(Entity<DisplacementActiveComponent> jaunter)
    {
        // Deferred so this is safe to call while enumerating in Update.
        RemCompDeferred<DisplacementActiveComponent>(jaunter);

        if (TerminatingOrDeleted(jaunter))
            return;

        var origin = jaunter.Comp.Origin;

        // The grid they started on may be gone by now (shuttle deleted, station destroyed, etc.).
        if (!origin.IsValid(EntityManager))
        {
            Popup.PopupClient(Loc.GetString("You cant seem to find your previous footing..."), jaunter, jaunter, PopupType.MediumCaution);
            return;
        }

        var xform = Transform(jaunter);
        _transform.SetCoordinates(jaunter, origin);
        _transform.AttachToGridOrMap(jaunter, xform);
    }
}
