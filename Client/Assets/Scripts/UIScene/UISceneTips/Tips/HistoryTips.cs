﻿using Game.Msg;
using SDK.Common;
using System.Collections.Generic;
using UnityEngine;

namespace Game.UI
{
    /**
     * @brief 历史记录提示
     */
    public class HistoryTips : TipsItemBase
    {
        protected HistoryTipsCard m_cardItem;
        protected List<HistoryTipsCard> m_list;

        public HistoryTips(SceneTipsData data)
            : base(data)
        {
            m_cardItem = new HistoryTipsCard();
            m_list = new List<HistoryTipsCard>();
        }

        public override void hide()
        {
            return;
            base.hide();
            UtilApi.Destroy(m_cardItem.getGameObject());

            foreach (HistoryTipsCard item in m_list)
            {
                UtilApi.Destroy(item.getGameObject());
            }
        }

        public void initWidget()
        {
            m_tipsItemRoot = UtilApi.TransFindChildByPObjAndPath(m_sceneTipsData.m_goRoot, "HistoryTips");
        }

        public void showTips(Vector3 pos, stRetBattleHistoryInfoUserCmd data)
        {
            show();

            // 显示卡牌历史提示
            TableCardItemBody cardTableItem = Ctx.m_instance.m_tableSys.getItem(TableID.TABLE_CARD, data.maincard.dwObjectID).m_itemBody as TableCardItemBody;

            GameObject tmpGO = Ctx.m_instance.m_modelMgr.getSceneCardModel((CardType)cardTableItem.m_type).getObject();
            if (tmpGO != null)
            {
                m_cardItem.setGameObject(UtilApi.Instantiate(tmpGO) as GameObject);
                m_cardItem.transform.SetParent(m_tipsItemRoot.transform, false);
                m_cardItem.transform.localPosition = new Vector3(-2.12f, 0, 0);
                m_cardItem.transform.localRotation = Quaternion.EulerRotation(270, 0, 0);
            }

            int idx = 0;
            for (idx = 0; idx < data.othercard.Length; ++idx)
            {
                cardTableItem = Ctx.m_instance.m_tableSys.getItem(TableID.TABLE_CARD, data.othercard[idx].dwObjectID).m_itemBody as TableCardItemBody;

                tmpGO = Ctx.m_instance.m_modelMgr.getSceneCardModel((CardType)cardTableItem.m_type).getObject();
                if (tmpGO != null)
                {
                    if (idx >= m_list.Count)
                    {
                        m_list.Add(new HistoryTipsCard());
                    }

                    m_list[idx].setGameObject(UtilApi.Instantiate(tmpGO) as GameObject);
                    m_list[idx].transform.SetParent(m_tipsItemRoot.transform, false);
                    m_cardItem.transform.localPosition = new Vector3(-2.12f + 1.5f * (1 + idx), 0, 0);
                    m_list[idx].transform.localRotation = Quaternion.EulerRotation(270, 0, 0);
                }
            }
        }
    }
}