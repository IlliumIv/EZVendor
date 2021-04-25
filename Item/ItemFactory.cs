﻿using System;
using System.Collections.Generic;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Nodes;
using EZVendor.Item.Filters;
using EZVendor.Item.Ninja;
// ReSharper disable ConstantConditionalAccessQualifier

namespace EZVendor.Item
{
    internal class ItemFactory : IItemFactory
    {
        private readonly GameController _gameController;
        private readonly INinjaProvider _ninjaProvider;
        private readonly bool _vendorTransmutes;
        private readonly bool _vendorScraps;
        private readonly bool _bypassBrokenItemMods;
        private readonly bool _vendorInfluenced;
        private readonly bool _lessGarbage;

        public ItemFactory(GameController gameController,
            INinjaProvider ninjaProvider,
            bool vendorTransmutes,
            bool vendorScraps,
            bool bypassBrokenItemMods,
            bool vendorInfluenced, 
            bool lessGarbage)
        {
            _gameController = gameController;
            _ninjaProvider = ninjaProvider;
            _vendorTransmutes = vendorTransmutes;
            _vendorScraps = vendorScraps;
            _bypassBrokenItemMods = bypassBrokenItemMods;
            _vendorInfluenced = vendorInfluenced;
            _lessGarbage = lessGarbage;
        }

        public Actions Evaluate(NormalInventoryItem normalInventoryItem)
        {
            try
            {
                #region keep invalid items

                var item = normalInventoryItem?.Item;
                if (item == null ||
                    item.Address == 0 ||
                    !item.IsValid ||
                    !item.HasComponent<Base>())
                    return Actions.Keep;
                
                if (item.GetComponent<Mods>()?.ItemMods == null)
                {
                    if (item?.GetComponent<Mods>()?.ItemRarity == ItemRarity.Rare) 
                        return Actions.Vendor;
                    if (_bypassBrokenItemMods && item?.GetComponent<Mods>()?.ItemRarity == ItemRarity.Unique)
                        return Actions.Vendor;
                }

                #endregion

                List<IEvaluate> filters;
                if (item.HasComponent<Mods>())
                {
                    filters = new List<IEvaluate>();
                    if (!_vendorInfluenced) filters.Add(new InfluencedFilter(_gameController, normalInventoryItem));
                    filters.Add(new VendorForScrolls(_gameController, normalInventoryItem, _vendorTransmutes, _vendorScraps));
                    filters.Add(new VendorForAltsFilter(_gameController, normalInventoryItem));
                    filters.Add(new SixSocketFilter(_gameController, normalInventoryItem));
                    filters.Add(new SixLinkFilter(_gameController, normalInventoryItem));
                    filters.Add(new MapFilter(_gameController, normalInventoryItem));
                    filters.Add(new EnchantedHelmetFilter(_gameController, normalInventoryItem));
                    filters.Add(new RareRingFilter(_gameController, normalInventoryItem, _lessGarbage));
                    filters.Add(new RareAmuletFilter(_gameController, normalInventoryItem, _lessGarbage));
                    filters.Add(new RareBeltFilter(_gameController, normalInventoryItem, _lessGarbage));
                    filters.Add(new RareGlovesFilter(_gameController, normalInventoryItem, _lessGarbage));
                    filters.Add(new RareBootsFilter(_gameController, normalInventoryItem, _lessGarbage));
                    filters.Add(new RareJewel(_gameController, normalInventoryItem, _lessGarbage));
                    filters.Add(new RareAbyssJewel(_gameController, normalInventoryItem, _lessGarbage));
                    filters.Add(new RareOneHanded(_gameController, normalInventoryItem, _lessGarbage));
                    filters.Add(new UniqueItemFilter(_gameController, normalInventoryItem, _ninjaProvider));
                }
                else
                    filters = new List<IEvaluate>
                    {
                        new VendorForScrolls(_gameController, normalInventoryItem, _vendorTransmutes, _vendorScraps),
                    };

                #region decide vendor/Keep

                var nVendor = 0;
                var nKeep = 0;
                foreach (var filter in filters)
                {
                    if (nKeep > 0) return Actions.Keep;
                    switch (filter.Evaluate())
                    {
                        case Actions.Vendor:
                            nVendor++;
                            break;
                        case Actions.Keep:
                            nKeep++;
                            break;
                        case Actions.CantDecide:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                return nVendor > 0 ? Actions.Vendor : Actions.Keep;

                #endregion
            }
            catch (Exception)
            {
                return Actions.Keep;
            }
        }
    }
}