// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Localisation;
using osu.Framework.Threading;
using osu.Game.Graphics;
using osu.Game.Graphics.Backgrounds;
using osu.Game.Online.Multiplayer;
using osu.Game.Screens.OnlinePlay.Components;

namespace osu.Game.Screens.OnlinePlay.Multiplayer.Match
{
    public class MultiplayerReadyButton : ReadyButton
    {
        public new Triangles Triangles => base.Triangles;

        [Resolved]
        private MultiplayerClient multiplayerClient { get; set; }

        [Resolved]
        private OsuColour colours { get; set; }

        [CanBeNull]
        private MultiplayerRoom room => multiplayerClient.Room;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            multiplayerClient.RoomUpdated += onRoomUpdated;
            onRoomUpdated();
        }

        private MultiplayerCountdown countdown;
        private DateTimeOffset countdownChangeTime;
        private ScheduledDelegate countdownUpdateDelegate;

        private void onRoomUpdated() => Scheduler.AddOnce(() =>
        {
            if (countdown != room?.Countdown)
            {
                countdown = room?.Countdown;
                countdownChangeTime = DateTimeOffset.Now;
            }

            scheduleNextCountdownUpdate();

            updateButtonText();
            updateButtonColour();
        });

        private void scheduleNextCountdownUpdate()
        {
            if (countdown != null)
            {
                // If a countdown is active, schedule relevant components to update on the next whole second.
                double timeToNextSecond = countdownTimeRemaining.TotalMilliseconds % 1000;

                countdownUpdateDelegate = Scheduler.AddDelayed(onCountdownTick, timeToNextSecond);
            }
            else
            {
                countdownUpdateDelegate?.Cancel();
                countdownUpdateDelegate = null;
            }

            void onCountdownTick()
            {
                updateButtonText();
                scheduleNextCountdownUpdate();
            }
        }

        private void updateButtonText()
        {
            if (room == null)
            {
                Text = "Ready";
                return;
            }

            var localUser = multiplayerClient.LocalUser;

            int countReady = room.Users.Count(u => u.State == MultiplayerUserState.Ready);
            int countTotal = room.Users.Count(u => u.State != MultiplayerUserState.Spectating);
            string countText = $"({countReady} / {countTotal} ready)";

            if (countdown != null)
            {
                string countdownText = $"Starting in {countdownTimeRemaining:mm\\:ss}";

                switch (localUser?.State)
                {
                    default:
                        Text = $"Ready ({countdownText.ToLowerInvariant()})";
                        break;

                    case MultiplayerUserState.Spectating:
                    case MultiplayerUserState.Ready:
                        Text = $"{countdownText} {countText}";
                        break;
                }
            }
            else
            {
                switch (localUser?.State)
                {
                    default:
                        Text = "Ready";
                        break;

                    case MultiplayerUserState.Spectating:
                    case MultiplayerUserState.Ready:
                        Text = room.Host?.Equals(localUser) == true
                            ? $"Start match {countText}"
                            : $"Waiting for host... {countText}";

                        break;
                }
            }
        }

        private TimeSpan countdownTimeRemaining
        {
            get
            {
                TimeSpan timeElapsed = DateTimeOffset.Now - countdownChangeTime;
                TimeSpan remaining;

                if (timeElapsed > countdown.TimeRemaining)
                    remaining = TimeSpan.Zero;
                else
                    remaining = countdown.TimeRemaining - timeElapsed;

                return remaining;
            }
        }

        private void updateButtonColour()
        {
            if (room == null)
            {
                setGreen();
                return;
            }

            var localUser = multiplayerClient.LocalUser;

            switch (localUser?.State)
            {
                default:
                    setGreen();
                    break;

                case MultiplayerUserState.Spectating:
                case MultiplayerUserState.Ready:
                    if (room?.Host?.Equals(localUser) == true && room.Countdown == null)
                        setGreen();
                    else
                        setYellow();

                    break;
            }

            void setYellow()
            {
                BackgroundColour = colours.YellowDark;
                Triangles.ColourDark = colours.YellowDark;
                Triangles.ColourLight = colours.Yellow;
            }

            void setGreen()
            {
                BackgroundColour = colours.Green;
                Triangles.ColourDark = colours.Green;
                Triangles.ColourLight = colours.GreenLight;
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (multiplayerClient != null)
                multiplayerClient.RoomUpdated -= onRoomUpdated;
        }

        public override LocalisableString TooltipText
        {
            get
            {
                if (room?.Countdown != null && multiplayerClient.IsHost && multiplayerClient.LocalUser?.State == MultiplayerUserState.Ready && !room.Settings.AutoStartEnabled)
                    return "Cancel countdown";

                return base.TooltipText;
            }
        }
    }
}
