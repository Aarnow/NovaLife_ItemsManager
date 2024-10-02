using ItemsManager.Entities;
using Life;
using Life.BizSystem;
using Life.InventorySystem;
using Life.Network;
using Life.UI;
using ModKit.Helper;
using ModKit.Interfaces;
using ModKit.Internal;
using ModKit.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using _menu = AAMenu.Menu;
using mk = ModKit.Helper.TextFormattingHelper;

namespace ItemsManager
{
    public class ItemsManager : ModKit.ModKit
    {
        public ItemsManager(IGameAPI api) : base(api)
        {
            PluginInformations = new PluginInformations(AssemblyHelper.GetName(), "1.0.0", "Aarnow");
        }

        public override void OnPluginInit()
        {
            base.OnPluginInit();

            Orm.RegisterTable<ItemsManager_Item>();
            InsertMenu();
            InitItemsManager();

            Logger.LogSuccess($"{PluginInformations.SourceName} v{PluginInformations.Version}", "initialisé");
        }

        public async void InitItemsManager()
        {
            try
            {
                await Task.Delay(1000); //wait register itemsManager tables
                List<ItemsManager_Item> itemsManager = await ItemsManager_Item.QueryAll();

                if(itemsManager != null)
                {
                    foreach (ItemsManager_Item item in itemsManager)
                    {
                        EditNovaItem(item);
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.LogWarning("InitItemsManager", ex.ToString());
            }
        }

        #region UTILS
        public void EditNovaItem(ItemsManager_Item item)
        {
            var currentItem = Nova.man.item.GetItem(item.ItemId);
            if (currentItem != null)
            {
                currentItem.itemName = item.ItemName;
                currentItem.maxSlotCount = item.MaxSlotCount;
                currentItem.weight = item.Weight;
                currentItem.buyable = item.IsBuyable;
                currentItem.resellable = item.IsResellable;
                currentItem.isStackable = item.IsStackable;
                item.DeserializeActivityAccess(item.ActivityAccess);
                currentItem.activityAccess = item.LActivityAccess;
                currentItem.isConsumable = item.IsConsumable;

                Food currentFood = currentItem as Food ?? null;

                if (currentFood != null)
                {
                    currentFood.food = item.Food;
                    currentFood.thirst = item.Thirst;
                    currentFood.isCuisinable = item.IsCuisinable;
                    currentFood.isAlcohol = item.IsAlcohol;
                    currentFood.alcoholValue = item.AlcoholValue;
                    currentFood.timeToBeCooked = item.TimeToBeCooked;
                    currentFood.timeToBeBurnt = item.TimeToBeBurnt;
                }
            }
        }
        #endregion

        public void InsertMenu()
        {
            _menu.AddAdminPluginTabLine(PluginInformations, 5, "ItemsManager", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                ItemsManagerPanel(player);
            });
        }

        public async void ItemsManagerPanel(Player player)
        {
            //Query
            List<ItemsManager_Item> items = await ItemsManager_Item.QueryAll();
            items = items.OrderBy(i => i.ItemId).ToList();
            //Déclaration
            Panel panel = PanelHelper.Create("ItemsManager - Liste des objets", UIPanel.PanelType.TabPrice, player, () => ItemsManagerPanel(player));

            //Corps
            if (items != null & items.Any())
            {
                foreach (var item in items)
                {
                    panel.AddTabLine($"{item.ItemName}", $"ID: [{item.ItemId}]", ItemUtils.GetIconIdByItemId(item.ItemId), _ => ItemsManagerDetailsPanel(player, item));
                }
            }
            else panel.AddTabLine("Aucune configuration", _ => { });

            //Boutons
            panel.NextButton("Sélectionner", () => panel.SelectTab());
            panel.NextButton("Ajouter", () => ItemsManagerCreatePanel(player));
            panel.AddButton("Retour", ui => AAMenu.AAMenu.menu.AdminPluginPanel(player, AAMenu.AAMenu.menu.AdminPluginTabLines));
            panel.CloseButton();

            //Affichage
            panel.Display();
        }

        public void ItemsManagerCreatePanel(Player player)
        {
            //DéclaratioI
            Panel panel = PanelHelper.Create("ItemsManager - Modifier un item", UIPanel.PanelType.Input, player, () => ItemsManagerCreatePanel(player));

            //Corps
            panel.inputPlaceholder = "ID de l'objet";

            //Boutons
            panel.PreviousButtonWithAction("Confirmer", async () =>
            {
                if (int.TryParse(panel.inputText, out int id))
                {
                    Item currentItem = ItemUtils.GetItemById(id);
                    if(currentItem != null)
                    {
                        var occurence = await ItemsManager_Item.Query(i => i.ItemId == id);
                        var itemExist = occurence != null && occurence.Any();
                        if (!itemExist)
                        {
                            ItemsManager_Item item = new ItemsManager_Item();
                            item.ItemId = currentItem.id;
                            item.ItemName = currentItem.itemName;
                            item.MaxSlotCount = currentItem.maxSlotCount;
                            item.Weight = currentItem.weight;
                            item.IsBuyable = currentItem.buyable;
                            item.IsResellable = currentItem.resellable;
                            item.IsStackable = currentItem.isStackable;
                            item.SerializeActivityAccess(currentItem.activityAccess);
                            item.IsConsumable = currentItem.isConsumable;

                            Food currentFood = currentItem as Food;

                            if (currentFood != null)
                            {
                                item.Food = currentFood.food;
                                item.Thirst = currentFood.thirst;
                                item.IsCuisinable = currentFood.isCuisinable;
                                item.IsAlcohol = currentFood.isAlcohol;
                                item.AlcoholValue = currentFood.alcoholValue;
                                item.TimeToBeCooked = currentFood.timeToBeCooked;
                                item.TimeToBeBurnt = currentFood.timeToBeBurnt;
                            }

                            if(await item.Save())
                            {
                                player.Notify("ItemsManager", "Nouvel objet enregistré avec succès", NotificationManager.Type.Success);
                                return true;
                            }else
                            {
                                player.Notify("ItemsManager", "Nous n'avons pas pu enregistrer ce nouvel objet", NotificationManager.Type.Error);
                                return false;
                            }
                        }
                        else
                        {
                            player.Notify("ItemsManager", "Cette objet est déjà dans la liste", NotificationManager.Type.Warning);
                            return false;
                        }
                    }
                    else
                    {
                        player.Notify("ItemsManager", "Aucun objet ne correspond à cette ID", NotificationManager.Type.Warning);
                        return false;
                    }
                }
                else
                {
                    player.Notify("ItemsManager", "Format incorrect", NotificationManager.Type.Warning);
                    return false;
                }
                
            });
            panel.PreviousButton();
            panel.CloseButton();

            //Affichage
            panel.Display();
        }

        public void ItemsManagerDetailsPanel(Player player, ItemsManager_Item item)
        {
            //Déclaration
            Panel panel = PanelHelper.Create("ItemsManager - Modifier un item", UIPanel.PanelType.TabPrice, player, () => ItemsManagerDetailsPanel(player, item));

            //Corps
            panel.AddTabLine($"{mk.Color($"[{item.ItemId}] {item.ItemName}", mk.Colors.Warning)}", _ =>
            {
                player.Notify("ItemsManager", "Vous ne pouvez pas modifier cette valeur.", NotificationManager.Type.Warning);
                panel.Refresh();
            });
            panel.AddTabLine($"{mk.Color("Sociétés autorisées:", mk.Colors.Info)} voir la liste", _ =>
            {
                ItemsManagerReadActivityAccessPanel(player, item);
            });
            panel.AddTabLine($"{mk.Color("Vendable:", mk.Colors.Info)} {(item.IsResellable ? $"{mk.Color("Oui", mk.Colors.Success)}" : $"{mk.Color("Non", mk.Colors.Error)}")}", async _ =>
            {
                item.IsResellable = !item.IsResellable;
                if(await item.Save())
                {
                    EditNovaItem(item);
                    panel.Refresh();
                }
                else player.Notify("ItemsManager", "Nous n'avons pas pu sauvegarder ce changement", NotificationManager.Type.Error);
            });
            panel.AddTabLine($"{mk.Color("Achetable:", mk.Colors.Info)} {(item.IsBuyable ? $"{mk.Color("Oui", mk.Colors.Success)}" : $"{mk.Color("Non", mk.Colors.Error)}")}", async _ =>
            {
                item.IsBuyable = !item.IsBuyable;
                if (await item.Save())
                {
                    EditNovaItem(item);
                    panel.Refresh();
                }
                else player.Notify("ItemsManager", "Nous n'avons pas pu sauvegarder ce changement", NotificationManager.Type.Error);
            });
            panel.AddTabLine($"{mk.Color("Poids:", mk.Colors.Info)} {item.Weight} gramme{(item.Weight > 1 ? "s" : "")}", _ =>
            {
                ItemsManagerEditWeightPanel(player, item);
            });

            panel.AddTabLine($"{mk.Color("Stackable:", mk.Colors.Info)} {(item.IsStackable ? $"{mk.Color("Oui", mk.Colors.Success)}" : $"{mk.Color("Non", mk.Colors.Error)}")}", async _ =>
            {
                item.IsStackable = !item.IsStackable;
                if (await item.Save())
                {
                    EditNovaItem(item);
                    panel.Refresh();
                }
                else player.Notify("ItemsManager", "Nous n'avons pas pu sauvegarder ce changement", NotificationManager.Type.Error);
            });
            if (item.IsStackable) panel.AddTabLine($"{mk.Color("Stockage max:", mk.Colors.Info)} {(item.MaxSlotCount > 0 ? item.MaxSlotCount.ToString() : $"infini")}", _ =>
            {
                ItemsManagerEditMaxSlotCountPanel(player, item);
            });

            panel.AddTabLine($"{mk.Color("Consommable:", mk.Colors.Info)} {(item.IsConsumable ? $"{mk.Color("Oui", mk.Colors.Success)}" : $"{mk.Color("Non", mk.Colors.Error)}")}", async _ =>
            {
                item.IsConsumable = !item.IsConsumable;
                if (await item.Save())
                {
                    EditNovaItem(item);
                    panel.Refresh();
                }
                else player.Notify("ItemsManager", "Nous n'avons pas pu sauvegarder ce changement", NotificationManager.Type.Error);
            });
            if (item.IsConsumable)
            {
                panel.AddTabLine($"{mk.Color("Gain de faim:", mk.Colors.Info)} {item.Food} %", _ =>
                {
                    ItemsManagerEditFoodPanel(player, item);
                });
                panel.AddTabLine($"{mk.Color("Gain de soif:", mk.Colors.Info)} {item.Thirst} %", _ =>
                {
                    ItemsManagerEditThirstPanel(player, item);
                });

                panel.AddTabLine($"{mk.Color("Alcoolisé:", mk.Colors.Info)} {(item.IsAlcohol ? $"{mk.Color("Oui", mk.Colors.Success)}" : $"{mk.Color("Non", mk.Colors.Error)}")}", async _ =>
                {
                    item.IsAlcohol = !item.IsAlcohol;
                    if (await item.Save())
                    {
                        EditNovaItem(item);
                        panel.Refresh();
                    }
                    else player.Notify("ItemsManager", "Nous n'avons pas pu sauvegarder ce changement", NotificationManager.Type.Error);
                });
                if (item.IsAlcohol) panel.AddTabLine($"{mk.Color("Degré d'alcool:", mk.Colors.Info)} {item.AlcoholValue} °", _ =>
                {
                    ItemsManagerEditAlcoholPanel(player, item);
                });

                panel.AddTabLine($"{mk.Color("Cuisinable:", mk.Colors.Info)} {(item.IsCuisinable ? $"{mk.Color("Oui", mk.Colors.Success)}" : $"{mk.Color("Non", mk.Colors.Error)}")}", async _ =>
                {
                    item.IsCuisinable = !item.IsCuisinable;
                    if (await item.Save())
                    {
                        EditNovaItem(item);
                        panel.Refresh();
                    }
                    else player.Notify("ItemsManager", "Nous n'avons pas pu sauvegarder ce changement", NotificationManager.Type.Error);
                });
                if (item.IsCuisinable)
                {
                    panel.AddTabLine($"{mk.Color("Temps de cuisson min:", mk.Colors.Info)} {item.TimeToBeCooked} secondes", _ =>
                    {
                        ItemsManagerEditTimeToBeCookedPanel(player, item);
                    });
                    panel.AddTabLine($"{mk.Color("Temps de cuisson max:", mk.Colors.Info)} {item.TimeToBeBurnt} secondes", _ =>
                    {
                        ItemsManagerEditTimeToBeBurntPanel(player, item);
                    });
                }
            }

            //Boutons
            panel.AddButton("Modifier", _ => panel.SelectTab());
            panel.PreviousButtonWithAction("Supprimer", async () =>
            {
                if(await item.Delete())
                {
                    player.Notify("ItemsManager", "Configuration d'objet supprimée avec succès", NotificationManager.Type.Success);
                    return true;
                } else
                {
                    player.Notify("ItemsManager", "Nous n'avons pas pu supprimer cette configuration d'objet", NotificationManager.Type.Error);
                    return false;
                }
            });
            panel.PreviousButton();
            panel.CloseButton();

            //Affichage
            panel.Display();
        }

        #region SETTERS
        public void ItemsManagerEditWeightPanel(Player player, ItemsManager_Item item)
        {
            //Déclaration
            Panel panel = PanelHelper.Create("ItemsManager - Modifier le poids", UIPanel.PanelType.Input, player, () => ItemsManagerEditWeightPanel(player, item));

            //Corps
            panel.TextLines.Add("Indiquer le poids de l'objet");
            panel.inputPlaceholder = $"{mk.Italic("exemple: 15")}";

            //Boutons
            panel.PreviousButtonWithAction("Confirmer", async () =>
            {
                if(int.TryParse(panel.inputText, out int value))
                {
                    if(value > 0)
                    {
                        item.Weight = value;
                        if (await item.Save())
                        {
                            EditNovaItem(item);
                            player.Notify("ItemsManager", $"Poids enregistré avec succès", NotificationManager.Type.Success);
                            return true;
                        } else
                        {
                            player.Notify("ItemsManager", "Nous n'avons pas pu sauvegarder ce changement", NotificationManager.Type.Error);
                            return false;
                        }
                    }
                    else
                    {
                        player.Notify("ItemsManager", "Veuillez renseigner une valeur positive", NotificationManager.Type.Warning);
                        return false;
                    }

                } else
                {
                    player.Notify("ItemsManager", "Format incorrect", NotificationManager.Type.Warning);
                    return false;
                }
            });
            panel.CloseButton();

            //Affichage
            panel.Display();
        }

        public void ItemsManagerEditMaxSlotCountPanel(Player player, ItemsManager_Item item)
        {
            //Déclaration
            Panel panel = PanelHelper.Create("ItemsManager - Modifier le stockage maximum", UIPanel.PanelType.Input, player, () => ItemsManagerEditMaxSlotCountPanel(player, item));

            //Corps
            panel.TextLines.Add("Indiquer le nombre stockable au maximum sur un slot");
            panel.inputPlaceholder = $"{mk.Italic("exemple: 15")}";

            //Boutons
            panel.PreviousButtonWithAction("Confirmer", async () =>
            {
                if (int.TryParse(panel.inputText, out int value))
                {
                    if (value > 0)
                    {
                        item.MaxSlotCount = value;
                        if (await item.Save())
                        {
                            EditNovaItem(item);
                            player.Notify("ItemsManager", $"Stockage maximum enregistré avec succès", NotificationManager.Type.Success);
                            return true;
                        }
                        else
                        {
                            player.Notify("ItemsManager", "Nous n'avons pas pu sauvegarder ce changement", NotificationManager.Type.Error);
                            return false;
                        }
                    }
                    else
                    {
                        player.Notify("ItemsManager", "Veuillez renseigner une valeur positive", NotificationManager.Type.Warning);
                        return false;
                    }

                }
                else
                {
                    player.Notify("ItemsManager", "Format incorrect", NotificationManager.Type.Warning);
                    return false;
                }
            });
            panel.CloseButton();

            //Affichage
            panel.Display();
        }

        public void ItemsManagerEditFoodPanel(Player player, ItemsManager_Item item)
        {
            //Déclaration
            Panel panel = PanelHelper.Create("ItemsManager - Modifier la faim", UIPanel.PanelType.Input, player, () => ItemsManagerEditFoodPanel(player, item));

            //Corps
            panel.TextLines.Add("Indiquer le pourcentage de faim rendu à la consommation");
            panel.inputPlaceholder = $"{mk.Italic("exemple: 15")}";

            //Boutons
            panel.PreviousButtonWithAction("Confirmer", async () =>
            {
                if (int.TryParse(panel.inputText, out int value))
                {
                    if (value > 0)
                    {
                        item.Food = value;
                        if (await item.Save())
                        {
                            EditNovaItem(item);
                            player.Notify("ItemsManager", $"Faim enregistrée avec succès", NotificationManager.Type.Success);
                            return true;
                        }
                        else
                        {
                            player.Notify("ItemsManager", "Nous n'avons pas pu sauvegarder ce changement", NotificationManager.Type.Error);
                            return false;
                        }
                    }
                    else
                    {
                        player.Notify("ItemsManager", "Veuillez renseigner une valeur positive", NotificationManager.Type.Warning);
                        return false;
                    }

                }
                else
                {
                    player.Notify("ItemsManager", "Format incorrect", NotificationManager.Type.Warning);
                    return false;
                }
            });
            panel.CloseButton();

            //Affichage
            panel.Display();
        }

        public void ItemsManagerEditThirstPanel(Player player, ItemsManager_Item item)
        {
            //Déclaration
            Panel panel = PanelHelper.Create("ItemsManager - Modifier la soif", UIPanel.PanelType.Input, player, () => ItemsManagerEditThirstPanel(player, item));

            //Corps
            panel.TextLines.Add("Indiquer le pourcentage de soif rendu à la consommation");
            panel.inputPlaceholder = $"{mk.Italic("exemple: 15")}";

            //Boutons
            panel.PreviousButtonWithAction("Confirmer", async () =>
            {
                if (int.TryParse(panel.inputText, out int value))
                {
                    if (value > 0)
                    {
                        item.Thirst = value;
                        if (await item.Save())
                        {
                            EditNovaItem(item);
                            player.Notify("ItemsManager", $"Soif enregistrée avec succès", NotificationManager.Type.Success);
                            return true;
                        }
                        else
                        {
                            player.Notify("ItemsManager", "Nous n'avons pas pu sauvegarder ce changement", NotificationManager.Type.Error);
                            return false;
                        }
                    }
                    else
                    {
                        player.Notify("ItemsManager", "Veuillez renseigner une valeur positive", NotificationManager.Type.Warning);
                        return false;
                    }

                }
                else
                {
                    player.Notify("ItemsManager", "Format incorrect", NotificationManager.Type.Warning);
                    return false;
                }
            });
            panel.CloseButton();

            //Affichage
            panel.Display();
        }

        public void ItemsManagerEditAlcoholPanel(Player player, ItemsManager_Item item)
        {
            //Déclaration
            Panel panel = PanelHelper.Create("ItemsManager - Modifier Degré d'alcool", UIPanel.PanelType.Input, player, () => ItemsManagerEditAlcoholPanel(player, item));

            //Corps
            panel.TextLines.Add("Indiquer le degré d'alcool");
            panel.inputPlaceholder = $"{mk.Italic("exemple: 8.2")}";

            //Boutons
            panel.PreviousButtonWithAction("Confirmer", async () =>
            {
                if (float.TryParse(panel.inputText, out float value))
                {
                    if (value > 0)
                    {
                        item.AlcoholValue = value;
                        if (await item.Save())
                        {
                            EditNovaItem(item);
                            player.Notify("ItemsManager", $"Degré d'alcool enregistré avec succès", NotificationManager.Type.Success);
                            return true;
                        }
                        else
                        {
                            player.Notify("ItemsManager", "Nous n'avons pas pu sauvegarder ce changement", NotificationManager.Type.Error);
                            return false;
                        }
                    }
                    else
                    {
                        player.Notify("ItemsManager", "Veuillez renseigner une valeur positive", NotificationManager.Type.Warning);
                        return false;
                    }

                }
                else
                {
                    player.Notify("ItemsManager", "Format incorrect", NotificationManager.Type.Warning);
                    return false;
                }
            });
            panel.CloseButton();

            //Affichage
            panel.Display();
        }

        public void ItemsManagerEditTimeToBeCookedPanel(Player player, ItemsManager_Item item)
        {
            //Déclaration
            Panel panel = PanelHelper.Create("ItemsManager - Modifier le temps de cuisson minimum", UIPanel.PanelType.Input, player, () => ItemsManagerEditTimeToBeCookedPanel(player, item));

            //Corps
            panel.TextLines.Add("Indiquer le temps de cuisson minimum");
            panel.inputPlaceholder = $"{mk.Italic("exemple: 2.5")}";

            //Boutons
            panel.PreviousButtonWithAction("Confirmer", async () =>
            {
                if (float.TryParse(panel.inputText, out float value))
                {
                    if (value > 0)
                    {
                        item.TimeToBeCooked = value;
                        if (await item.Save())
                        {
                            EditNovaItem(item);
                            player.Notify("ItemsManager", $"Temps de cuisson minimum enregistré avec succès", NotificationManager.Type.Success);
                            return true;
                        }
                        else
                        {
                            player.Notify("ItemsManager", "Nous n'avons pas pu sauvegarder ce changement", NotificationManager.Type.Error);
                            return false;
                        }
                    }
                    else
                    {
                        player.Notify("ItemsManager", "Veuillez renseigner une valeur positive", NotificationManager.Type.Warning);
                        return false;
                    }

                }
                else
                {
                    player.Notify("ItemsManager", "Format incorrect", NotificationManager.Type.Warning);
                    return false;
                }
            });
            panel.CloseButton();

            //Affichage
            panel.Display();
        }

        public void ItemsManagerEditTimeToBeBurntPanel(Player player, ItemsManager_Item item)
        {
            //Déclaration
            Panel panel = PanelHelper.Create("ItemsManager - Modifier le temps de cuisson maximum", UIPanel.PanelType.Input, player, () => ItemsManagerEditTimeToBeBurntPanel(player, item));

            //Corps
            panel.TextLines.Add("Indiquer le temps de cuisson maximum");
            panel.inputPlaceholder = $"{mk.Italic("exemple: 3.5")}";

            //Boutons
            panel.PreviousButtonWithAction("Confirmer", async () =>
            {
                if (float.TryParse(panel.inputText, out float value))
                {
                    if (value > 0)
                    {
                        item.TimeToBeBurnt = value;
                        if (await item.Save())
                        {
                            EditNovaItem(item);
                            player.Notify("ItemsManager", $"Temps de cuisson maximum enregistré avec succès", NotificationManager.Type.Success);
                            return true;
                        }
                        else
                        {
                            player.Notify("ItemsManager", "Nous n'avons pas pu sauvegarder ce changement", NotificationManager.Type.Error);
                            return false;
                        }
                    }
                    else
                    {
                        player.Notify("ItemsManager", "Veuillez renseigner une valeur positive", NotificationManager.Type.Warning);
                        return false;
                    }

                }
                else
                {
                    player.Notify("ItemsManager", "Format incorrect", NotificationManager.Type.Warning);
                    return false;
                }
            });
            panel.CloseButton();

            //Affichage
            panel.Display();
        }

        public void ItemsManagerReadActivityAccessPanel(Player player, ItemsManager_Item item)
        {
            //Déclaration
            Panel panel = PanelHelper.Create("ItemsManager - Liste des sociétés autorisées", UIPanel.PanelType.TabPrice, player, () => ItemsManagerReadActivityAccessPanel(player, item));

            //Corps
            foreach (Activity.Type activity in Enum.GetValues(typeof(Activity.Type)))
            {
                bool condition = item.LActivityAccess.Contains(activity);
                panel.AddTabLine($"{mk.Color($"{activity}", (condition ? mk.Colors.Success : mk.Colors.Error))}", async _ =>
                {
                    if(condition) item.LActivityAccess.Remove(activity);
                    else item.LActivityAccess.Add(activity);

                    item.SerializeActivityAccess(item.LActivityAccess);
                    if (await item.Save())
                    {
                        EditNovaItem(item);
                        player.Notify("ItemsManager", $"Société {activity} {(condition ? "retirée" : "ajouteé")} avec succès", NotificationManager.Type.Success);
                    }
                    else
                    {
                        player.Notify("ItemsManager", "Nous n'avons pas pu sauvegarder ce changement", NotificationManager.Type.Error);
                    }
                    panel.Refresh();
                });
            }


            //Boutons
            panel.AddButton("Sélectionner", _ => panel.SelectTab());
            panel.PreviousButton();
            panel.CloseButton();

            //Affichage
            panel.Display();
        }
        #endregion
    }
}
