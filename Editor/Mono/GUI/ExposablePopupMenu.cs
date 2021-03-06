// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using System.IO;

using UnityEditorInternal;
using UnityEngine;


namespace UnityEditor
{
    internal class ExposablePopupMenu
    {
        public class ItemData
        {
            public ItemData(GUIContent content, GUIStyle style, bool on, bool enabled, object userData)
            {
                m_GUIContent = content;
                m_Style = style;
                m_On = on;
                m_Enabled = enabled;
                m_UserData = userData;
            }

            public GUIContent m_GUIContent;
            public GUIStyle m_Style;
            public bool m_On;
            public bool m_Enabled;
            public object m_UserData;
            public float m_Width;
            public float m_Height;
        }

        public class PopupButtonData
        {
            public PopupButtonData(GUIContent content, GUIStyle style)
            {
                m_GUIContent = content;
                m_Style = style;
            }

            public GUIContent m_GUIContent;
            public GUIStyle m_Style;
        }

        List<ItemData> m_Items;
        float m_WidthOfButtons;
        float m_ItemSpacing;
        PopupButtonData m_PopupButtonData;
        Vector2 m_PopupButtonSize = Vector2.zero;
        float m_MinWidthOfPopup;
        System.Action<ItemData> m_SelectionChangedCallback = null; // <userData>

        public void Init(List<ItemData> items, float itemSpacing, float minWidthOfPopup, PopupButtonData popupButtonData, System.Action<ItemData> selectionChangedCallback)
        {
            m_Items = items;
            m_ItemSpacing = itemSpacing;
            m_PopupButtonData = popupButtonData;
            m_SelectionChangedCallback = selectionChangedCallback;
            m_MinWidthOfPopup = minWidthOfPopup;
            CalcWidths();
        }

        public float OnGUI(Rect rect)
        {
            if (rect.width >= m_WidthOfButtons && rect.width > m_MinWidthOfPopup)
            {
                Rect buttonRect = rect;

                // Show as buttons
                foreach (var item in m_Items)
                {
                    buttonRect.width = item.m_Width;
                    buttonRect.y = rect.y + (rect.height - item.m_Height) / 2;
                    buttonRect.height = item.m_Height;

                    EditorGUI.BeginChangeCheck();

                    using (new EditorGUI.DisabledScope(!item.m_Enabled))
                    {
                        GUI.Toggle(buttonRect, item.m_On, item.m_GUIContent, item.m_Style);
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        SelectionChanged(item);
                        GUIUtility.ExitGUI(); // To make sure we can survive if m_Buttons are reallocated in the callback we exit gui
                    }

                    buttonRect.x += item.m_Width + m_ItemSpacing;
                }

                return m_WidthOfButtons;
            }
            else
            {
                var dropDownRect = rect;

                // Show as popup
                if (m_PopupButtonSize.x < rect.width)
                    dropDownRect.width = m_PopupButtonSize.x;

                dropDownRect.height = m_PopupButtonSize.y;
                dropDownRect.y = rect.y + (rect.height - dropDownRect.height) / 2;

                //if (GUI.Button (rect, m_PopupButtonData.m_GUIContent, m_PopupButtonData.m_Style))
                if (EditorGUI.DropdownButton(dropDownRect, m_PopupButtonData.m_GUIContent, FocusType.Passive, m_PopupButtonData.m_Style))
                    PopUpMenu.Show(dropDownRect, m_Items, this);

                return m_PopupButtonSize.x;
            }
        }

        void CalcWidths()
        {
            // Buttons
            m_WidthOfButtons = 0f;
            foreach (var item in m_Items)
            {
                var itemSize = item.m_Style.CalcSize(item.m_GUIContent);
                item.m_Width = itemSize.x;
                item.m_Height = itemSize.y;

                m_WidthOfButtons += item.m_Width;
            }
            m_WidthOfButtons += (m_Items.Count - 1) * m_ItemSpacing;

            // Popup
            m_PopupButtonSize = m_PopupButtonData.m_Style.CalcSize(m_PopupButtonData.m_GUIContent);
        }

        void SelectionChanged(ItemData item)
        {
            if (m_SelectionChangedCallback != null)
                m_SelectionChangedCallback(item);
            else
                Debug.LogError("Callback is null");
        }

        internal class PopUpMenu
        {
            static List<ItemData> m_Data;
            static ExposablePopupMenu m_Caller;

            static internal void Show(Rect activatorRect, List<ItemData> buttonData, ExposablePopupMenu caller)
            {
                m_Data = buttonData;
                m_Caller = caller;

                GenericMenu menu = new GenericMenu();
                foreach (ItemData item in m_Data)
                    if (item.m_Enabled)
                        menu.AddItem(item.m_GUIContent, item.m_On, SelectionCallback, item);
                    else
                        menu.AddDisabledItem(item.m_GUIContent);

                menu.DropDown(activatorRect);
            }

            static void SelectionCallback(object userData)
            {
                ItemData item = (ItemData)userData;
                m_Caller.SelectionChanged(item);

                // Cleanup
                m_Caller = null;
                m_Data = null;
            }
        }
    }
} // end namespace UnityEditor
