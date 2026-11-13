using UnityEngine;

public interface IItem
{
    string ItemName { get; }
    Sprite ItemIcon { get; }
    string GetTooltipStatsText();
}

