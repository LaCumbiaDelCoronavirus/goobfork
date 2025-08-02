using Content.Goobstation.Server.Ordnance.TTV;
using Content.Goobstation.Shared.Ordnance.TTV;
using Content.Shared.Containers.ItemSlots;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Goobstation.Client.Ordnance.TTV;

public sealed class TTVSystem : SharedTTVSystem
{
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TTVComponent, ComponentInit>(TTVInit);
        SubscribeLocalEvent<TTVComponent, AppearanceChangeEvent>(UpdateAppearance);

        SubscribeLocalEvent<TTVComponent, EntInsertedIntoContainerMessage>(OnTankInserted);
        SubscribeLocalEvent<TTVComponent, EntRemovedFromContainerMessage>(OnTankRemoved);
    }

    private void TTVInit(Entity<TTVComponent> ttv, ref ComponentInit args)
    {
        var ttvSpriteEntity = new Entity<SpriteComponent?>(ttv, null);
        if (!Resolve(ttvSpriteEntity, ref ttvSpriteEntity.Comp))
            return;

        if (!TryComp<ItemSlotsComponent>(ttv, out var slotsComponent))
            return;

        foreach (var (slotId, _) in slotsComponent.Slots)
            _spriteSystem.LayerMapReserve(ttvSpriteEntity, slotId);
    }

    private void UpdateAppearance(Entity<TTVComponent> ttv, ref AppearanceChangeEvent args)
        => UpdateAppearance((ttv.Owner, null, null));

    private void UpdateAppearance(EntityUid ttv)
        => UpdateAppearance((ttv, null, null));

    private void UpdateAppearance(Entity<ItemSlotsComponent?, SpriteComponent?> ttv)
    {
        if (!Resolve(ttv, ref ttv.Comp1, logMissing: false) || !Resolve(ttv, ref ttv.Comp2, logMissing: false))
            return;

        var slotsComponent = ttv.Comp1;
        var ttvSpriteEntity = new Entity<SpriteComponent?>(ttv, ttv.Comp2);

        foreach (var (slotId, slot) in slotsComponent.Slots)
        {
            var slotOccupied = slot.HasItem;
            _spriteSystem.LayerSetVisible(ttvSpriteEntity, slotId, slotOccupied);

            if (!slotOccupied || !TTVCompatibleQuery.TryComp(slot.Item, out var itemCompatibleComponent))
                continue;

            _spriteSystem.LayerSetSprite(ttvSpriteEntity, slotId, new SpriteSpecifier.Rsi(itemCompatibleComponent.InsertedTexture!.Value, itemCompatibleComponent.InsertedState));
        }
    }

    private void OnTankInserted(Entity<TTVComponent> ttv, ref EntInsertedIntoContainerMessage _)
        => UpdateAppearance(ttv);

    private void OnTankRemoved(Entity<TTVComponent> ttv, ref EntRemovedFromContainerMessage _)
        => UpdateAppearance(ttv);
}
