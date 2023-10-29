using HarmonyLib;
using ShinyShoe;
using ShinyShoe.Ares;
using ShinyShoe.EcsEventSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace InkyBubbleTweaks.Logbook
{
    [HarmonyPatch(typeof(LogbookScreenVisual))]
    public static class Logbook_Patches
    {
        private static ArtemisTextButton seasonRewardsButton;
        private static GameObject seasonRewardsContent;

        private static LogbookScreenVisual.Tab SeasonRewardsTab = (LogbookScreenVisual.Tab)100;

        //public void Set(
        //  EntityHandle localPlayerHandle,
        //  VestigeSetState vestigeSetState,
        //  ILocalizationParameterContext localizationContext,
        //  Vector3 defaultPos,
        //  Vector3 flippedPos,
        //  TooltipAnchor anchor)
        [HarmonyPatch(nameof(LogbookScreenVisual.ApplyScreenInput))]
        [HarmonyPrefix]
        public static bool Input(ref bool __result, LogbookScreenVisual __instance, InputSignal inputSignal)
        {
            if (seasonRewardsButton.TryTrigger(inputSignal))
            {
                __instance.SetNextTab(SeasonRewardsTab);
                __result = true;
                return false;
            }
            return true;
        }

        [HarmonyPatch(nameof(LogbookScreenVisual.SetNextTab))]
        [HarmonyPostfix]
        public static void SetNextTab(LogbookScreenVisual __instance, LogbookScreenVisual.Tab nextTab)
        {
            try
            {
                __instance.SetTabActive(seasonRewardsButton, __instance.questsGleam, seasonRewardsContent, SeasonRewardsTab, false);
            }
            catch (Exception)
            {
                //this is ok - just means we haven't init yet
            }
        }

        [HarmonyPatch(nameof(LogbookScreenVisual.Initialize))]
        [HarmonyPostfix]
        public static void Initialize(LogbookScreenVisual __instance,
                                      EntityHandle entityHandle,
                                      ClientAssetDB clientAssetDb,
                                      LocalizationSystem.State.IReadonly localizationRo,
                                      EventDB eventDb,
                                      PlayerSeasonWrapper playerSeasonWrapper)
        {
            try
            {
                var tabArea = __instance.gameObject.transform.Find("Tabs Area").transform;
                var tabRect = (tabArea as RectTransform);

                var contentArea = __instance.gameObject.transform.Find("4:3 Container").transform;

                var bgSprite = __instance.questsInnerAreaUI.questCategoryPrefab.questItemShortPrefab.bgImage.sprite;

                // This is poo for people on 1280 - how to fix?
                tabRect.sizeDelta = new(1470, tabRect.sizeDelta.y);

                var buttonObject = GameObject.Instantiate(tabArea.Find("Season Button").transform);
                var button = buttonObject.GetComponentInChildren<ArtemisTextButton>();
                seasonRewardsButton = button;

                button.label.text = "Season Rewards";
                buttonObject.transform.SetParent(tabArea);
                buttonObject.transform.SetSiblingIndex(2);

                const int itemWidth = 900;

                var cosmeticPreviewPrefab = GameObject.Instantiate(contentArea.Find("SeasonInnerArea/CosmeticReward").gameObject);

                var content = GameObject.Instantiate(contentArea.Find("QuestsInnerArea").gameObject);
                seasonRewardsContent = content;

                GameObject.Destroy(content.GetComponentInChildren<QuestsInnerAreaUI>());
                var contentRoot = content.transform as RectTransform;
                var contentBG = contentRoot.Find("Categories Background") as RectTransform;
                contentBG.Find("Quest Set Category Title").GetComponentInChildren<TextMeshProUGUI>().text = "Rewards";
                var contentPanel = contentBG.Find("Inner Area/Categories Scroll View/Viewport/Content");

                var children = new List<GameObject>();
                foreach (Transform child in contentPanel) children.Add(child.gameObject);
                children.ForEach(GameObject.Destroy);

                GameObject.Destroy(contentRoot.Find("Set Buttons Scroll View").gameObject);

                GameObject rewardPrefab = new("reward_prefab", typeof(RectTransform));
                var rewardRect = rewardPrefab.transform as RectTransform;
                rewardRect.sizeDelta = new(0, 0);

                GameObject rewardContainer = new("container", typeof(RectTransform));
                rewardContainer.AddComponent<Image>().sprite = bgSprite;
                rewardContainer.transform.SetParent(rewardRect, false);
                rewardContainer.FillParent();

                cosmeticPreviewPrefab.transform.SetParent(rewardContainer.transform, false);

                var locEvent = cosmeticPreviewPrefab.transform.Find("Count Label").GetComponent<LocalizeStringEvent>();
                LocalizedString locHelper = new(locEvent.StringReference.TableReference, locEvent.StringReference.TableEntryReference);

                GameObject rewardLevel = new("level", typeof(RectTransform), typeof(TextMeshProUGUI));
                rewardLevel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
                rewardLevel.GetComponent<TextMeshProUGUI>().color = new(0.2f, 0.2f, 0.2f);
                rewardLevel.transform.SetParent(rewardContainer.transform, false);

                GameObject rewardLabel = new("label", typeof(RectTransform), typeof(TextMeshProUGUI));
                rewardLabel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
                rewardLabel.transform.SetParent(rewardContainer.transform, false);

                var rewardLayout = rewardPrefab.AddComponent<LayoutElement>();
                rewardLayout.preferredHeight = 80;
                rewardLayout.minHeight = 80;
                rewardLayout.preferredWidth = itemWidth;
                rewardLayout.minWidth = itemWidth;

                var contentLayout = contentPanel.gameObject.AddComponent<GridLayoutGroup>();
                contentLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                contentLayout.constraintCount = 1;
                contentLayout.cellSize = new(itemWidth, 80);
                contentLayout.startAxis = GridLayoutGroup.Axis.Vertical;
                contentLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
                contentLayout.childAlignment = TextAnchor.MiddleCenter;
                contentLayout.spacing = new(0, 8);

                var contentFitter = contentPanel.gameObject.AddComponent<ContentSizeFitter>();
                contentFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;


                var rewards = playerSeasonWrapper.currentSeasonData.SeasonRewardListData.SeasonRewardsForLevel;

                content.transform.SetParent(contentArea, false);
                content.transform.SetSiblingIndex(2);

                contentBG.sizeDelta = new(contentBG.sizeDelta.x + 200, contentBG.sizeDelta.y);

                int currentLevel = playerSeasonWrapper.seasonRankProgressWrapper.currentProgress.rank;

                Transform lastHave = null;

                foreach (var reward in rewards.Where(r => r.freeReward != null))
                {
                    var view = GameObject.Instantiate(rewardPrefab);
                    view.transform.SetParent(contentPanel, false);

                    var preview = view.GetComponentInChildren<InventoryItemUI>();
                    preview.Initialize(InventoryLocation.None, clientAssetDb, eventDb);
                    preview.ResetItem();

                    string rewardName = "";
                    var gift = reward.freeReward;
                    preview.SetSeasonReward(gift, entityHandle);

                    if (gift.CurrencyAmount > 0)
                        rewardName = $"{gift.currencyAmount} Vault Dust";
                    if (gift.trinketCurrencyAmount > 0)
                        rewardName = $"{gift.trinketCurrencyAmount} Trinket Keys";
                    if (gift.cosmeticData != null)
                    {
                        locHelper.TableReference = localizationRo.FindTable(gift.CosmeticData.NameKey);
                        locHelper.TableEntryReference = gift.CosmeticData.NameKey;
                        locHelper.RefreshString();
                        rewardName = $"{locHelper.GetLocalizedString()} (cosmetic)";
                    }
                    if (gift.equipmentData != null)
                        rewardName = $"{gift.equipmentData.NameKey} (vestige)";

                    var levelRect = view.transform.Find("container/level").transform as RectTransform;
                    var level = levelRect.GetComponent<TextMeshProUGUI>();
                    level.text = reward.Level.ToString();
                    levelRect.SetAnchor(0.15, 0.3, 0.5, 0.5);
                    levelRect.anchoredPosition = Vector2.zero;
                    levelRect.sizeDelta = Vector2.zero;

                    var labelRect = view.transform.Find("container/label").transform as RectTransform;
                    var label = labelRect.GetComponent<TextMeshProUGUI>();
                    label.text = rewardName;
                    labelRect.SetAnchor(0.28, 1, 0.5, 0.5);
                    labelRect.anchoredPosition = Vector2.zero;
                    labelRect.sizeDelta = Vector2.zero;

                    var previewRect = preview.transform as RectTransform;
                    previewRect.anchoredPosition = new(50, 0);
                    previewRect.SetAnchor(0, 0, 0.5, 0.5);

                    bool have = currentLevel >= reward.Level;
                    if (have)
                    {
                        label.color = Color.blue;
                        lastHave = view.transform;
                    }
                    else
                    {
                        label.color = Color.black;
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.log.LogError(ex.Message);
            }
        }
    }

}
