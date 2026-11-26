using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>
/// マウスで UI を押すための EventSystem まわりの共通処理
/// </summary>
public static class UiEventSystem
{
    /// <summary>
    /// シーンに EventSystem がなければ作る（これがないとクリックが届かない）
    /// </summary>
    public static void Ensure()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
        InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
        inputModule.AssignDefaultActions();
    }

    /// <summary>
    /// クリックしたボタンが選択されたままだと、Space や Enter でもう一度押されてしまうので解除する
    /// </summary>
    public static void ClearSelection()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
