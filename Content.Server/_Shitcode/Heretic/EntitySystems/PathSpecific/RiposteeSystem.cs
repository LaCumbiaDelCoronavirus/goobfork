// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2024 username <113782077+whateverusername0@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 whateverusername0 <whateveremail>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Hands.Systems;
using Content.Server.Heretic.Components.PathSpecific;
using Content.Server.Popups;
using Content.Shared.Damage;
using Content.Shared.Weapons.Melee;

namespace Content.Server.Heretic.EntitySystems.PathSpecific;

public sealed partial class RiposteeSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _dmg = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RiposteeComponent, BeforeDamageChangedEvent>(OnBeforeDamageChange);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var eqe = EntityQueryEnumerator<RiposteeComponent>();
        while (eqe.MoveNext(out var uid, out var rip))
        {
            if (!uid.IsValid())
                continue;

            if (rip.CanRiposte)
                continue;

            rip.Timer -= frameTime;

            if (rip.Timer <= 0)
            {
                rip.Timer = rip.Cooldown;

                rip.CanRiposte = true;
                _popup.PopupEntity(Loc.GetString("heretic-riposte-available"), uid, uid);
            }
        }
    }

    private void OnBeforeDamageChange(Entity<RiposteeComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (!args.Damage.AnyPositive() // is healing
        || !ent.Comp.CanRiposte // can't riposte
        || !HasComp<MeleeWeaponComponent>(_hands.GetActiveItem(ent.Owner))) // not holding a melee weapon
            return;

        args.Cancelled = true;
        if (HasComp<DamageableComponent>(args.Origin))
            _dmg.TryChangeDamage(args.Origin, args.Damage, true); // back to sender

        ent.Comp.CanRiposte = false;
        _popup.PopupEntity(Loc.GetString("heretic-riposte-used"), ent);
    }
}