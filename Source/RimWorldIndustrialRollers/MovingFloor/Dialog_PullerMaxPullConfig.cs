using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace MovingFloor;

internal sealed class Dialog_PullerMaxPullConfig : Window
{
    private readonly MovingRailPuller _pullerInstance;

    private readonly float _windowInitHourInterval;

    private readonly float _windowInitMaxPulls;
    private float _hourInterval;
    private float _maxPulls;

    public Dialog_PullerMaxPullConfig(int everyHours, int maxPulls, MovingRailPuller movingRailPuller)
    {
        _maxPulls = maxPulls;
        _windowInitMaxPulls = maxPulls;
        _hourInterval = everyHours;
        _windowInitHourInterval = everyHours;
        _pullerInstance = movingRailPuller;
        closeOnClickedOutside = true;
    }

    private Vector2 RequestedTabSize => new Vector2(200f, 200f);

    public override Vector2 InitialSize
    {
        get
        {
            var requestedTabSize = RequestedTabSize;
            if (requestedTabSize.y > (double)(UI.screenHeight - 35))
            {
                requestedTabSize.y = UI.screenHeight - 35;
            }

            if (requestedTabSize.x > (double)UI.screenWidth)
            {
                requestedTabSize.x = UI.screenWidth;
            }

            return requestedTabSize;
        }
    }

    public override void DoWindowContents(Rect inRect)
    {
        var rect = new Rect(0f, 0f, 150f, 150f);
        var listing_Standard = new Listing_Standard
        {
            ColumnWidth = rect.width
        };
        listing_Standard.Begin(rect);
        if (_hourInterval > 24.0)
        {
            _hourInterval = 24f;
        }

        var rightLabel = Math.Round(_hourInterval, 0).ToString("F0");
        listing_Standard.LabelDouble("MovingRail_Every_Hours".Translate() + ": ", rightLabel);
        _hourInterval = listing_Standard.Slider(_hourInterval, 1f, 24f);
        if (_maxPulls > 100.0)
        {
            _maxPulls = 100f;
        }

        var rightLabel2 = Math.Round(_maxPulls, 0).ToString("F0");
        if (Math.Abs((double)_maxPulls) < 0.9 || _maxPulls > 100.0)
        {
            rightLabel2 = "MovingRail_Unlimited".Translate();
        }

        listing_Standard.LabelDouble("MovingRail_Pulls_To_Do".Translate() + ": ", rightLabel2);
        _maxPulls = listing_Standard.Slider(_maxPulls, 0f, 100f);
        listing_Standard.End();
    }

    public override void PreClose()
    {
        base.PreClose();
        if (_maxPulls == _windowInitMaxPulls && _hourInterval == _windowInitHourInterval)
        {
            return;
        }

        Messages.Message("MovingRail_PullerSettingsChanged".Translate(), MessageTypeDefOf.PositiveEvent);
        _pullerInstance.UpdatePullsPerHour((int)Math.Round(_hourInterval, 0), (int)Math.Round(_maxPulls, 0));
    }
}