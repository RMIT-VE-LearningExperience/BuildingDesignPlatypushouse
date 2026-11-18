using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Panel coordinates are millimetres at the chosen world-space canvas scale.
public sealed class TourPanel : MonoBehaviour
{
    public const int Layer = 5;
    public static readonly Color Ink = new Color32(16, 36, 46, 255);
    static readonly Color Tile = new Color32(42, 73, 86, 255);
    static readonly Color Selected = new Color32(25, 108, 95, 255);
    static readonly Color Hover = new Color32(33, 114, 152, 255);
    readonly Dictionary<Collider, (Image face, Action action)> buttons = new();
    readonly List<GameObject> content = new();
    Collider hot, selected;
    RectTransform root;

    public static TourPanel Create(string name, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(Image), typeof(TourPanel));
        var p = go.GetComponent<TourPanel>();
        p.root = go.GetComponent<RectTransform>();
        p.root.sizeDelta = size;
        p.root.localScale = Vector3.one * .001f;
        go.layer = Layer;
        go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        go.GetComponent<Image>().color = Ink;
        return p;
    }

    RectTransform Item(string name, Vector2 point, Vector2 size, Transform parent = null)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = Layer;
        rect.SetParent(parent == null ? root : parent, false);
        rect.sizeDelta = size;
        rect.anchoredPosition = point;
        if (parent == null) content.Add(rect.gameObject);
        return rect;
    }

    public void Clear()
    {
        foreach (var item in content) { item.SetActive(false); Destroy(item); }
        content.Clear();
        buttons.Clear();
        hot = selected = null;
    }

    public TextMeshProUGUI Text(string value, float x, float y, float width, float height, float size,
        bool centered = false, Transform parent = null)
    {
        var obj = Item("Label", new Vector2(x,y), new Vector2(width,height),parent);
        var label = obj.gameObject.AddComponent<TextMeshProUGUI>();
        label.fontSize = size;
        label.color = Color.white;
        label.alignment = centered ? TextAlignmentOptions.Center : TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false;
        label.text = value;
        return label;
    }

    public RectTransform Picture(Texture2D texture, Vector2 point, Vector2 size)
    {
        var rect = Item("Plan", point, size);
        rect.gameObject.AddComponent<RawImage>().texture = texture;
        return rect;
    }

    public Collider Button(string label, float x, float y, float width, Action action, bool active = false)
    {
        const float height = 78;
        var rect = Item(label, new Vector2(x,y), new Vector2(width,height));
        var face = rect.gameObject.AddComponent<Image>();
        var box = rect.gameObject.AddComponent<BoxCollider>();
        box.size = new Vector3(width,height,8);
        Text(label,0,0,width-10,height,32,true,rect);
        buttons[box] = (face,action);
        if (active) selected = box;
        Paint(box);
        return box;
    }

    void Paint(Collider c)
    {
        if (c != null && buttons.TryGetValue(c,out var b))
            b.face.color = c == hot ? Hover : c == selected ? Selected : Tile;
    }

    public bool Point(Collider target)
    {
        var previous = hot;
        hot = target != null && buttons.ContainsKey(target) ? target : null;
        Paint(previous);
        Paint(hot);
        return hot != null;
    }

    public void Press(Collider c)
    {
        if (c != null && buttons.TryGetValue(c,out var b)) b.action();
    }
}