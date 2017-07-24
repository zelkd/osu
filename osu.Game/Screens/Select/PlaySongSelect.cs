﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System.Collections.Generic;
using System.Linq;
using OpenTK.Input;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;
using osu.Framework.Screens;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Overlays.Mods;
using osu.Game.Rulesets.Mods;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Play;
using osu.Game.Screens.Ranking;

namespace osu.Game.Screens.Select
{
    public class PlaySongSelect : SongSelect
    {
        private OsuScreen player;
        private readonly ModSelectOverlay modSelect;
        private readonly BeatmapDetailArea beatmapDetails;
        private IEnumerable<Mod> originalMods;
        private bool controlPressed;

        public PlaySongSelect()
        {
            FooterPanels.Add(modSelect = new ModSelectOverlay
            {
                RelativeSizeAxes = Axes.X,
                Origin = Anchor.BottomCentre,
                Anchor = Anchor.BottomCentre,
            });

            LeftContent.Add(beatmapDetails = new BeatmapDetailArea
            {
                RelativeSizeAxes = Axes.Both,
                Padding = new MarginPadding { Top = 10, Right = 5 },
            });

            beatmapDetails.Leaderboard.ScoreSelected += s => Push(new Results(s));
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            Footer.AddButton(@"mods", colours.Yellow, modSelect.ToggleVisibility, Key.F1, float.MaxValue);

            BeatmapOptions.AddButton(@"Remove", @"from unplayed", FontAwesome.fa_times_circle_o, colours.Purple, null, Key.Number1);
            BeatmapOptions.AddButton(@"Clear", @"local scores", FontAwesome.fa_eraser, colours.Purple, null, Key.Number2);
            BeatmapOptions.AddButton(@"Edit", @"Beatmap", FontAwesome.fa_pencil, colours.Yellow, () =>
            {
                ValidForResume = false;
                Push(new Editor());
            }, Key.Number3);

            Beatmap.ValueChanged += beatmap_ValueChanged;
        }

        private void beatmap_ValueChanged(WorkingBeatmap beatmap)
        {
            if (!IsCurrentScreen) return;

            beatmap.Mods.BindTo(modSelect.SelectedMods);

            beatmapDetails.Beatmap = beatmap;

            if (beatmap.Track != null)
                beatmap.Track.Looping = true;
        }

        protected override void OnResuming(Screen last)
        {
            player = null;

            modSelect.SelectedMods.Value = originalMods;
            originalMods = null;

            Beatmap.Value.Track.Looping = true;

            base.OnResuming(last);
        }

        protected override void OnSuspending(Screen next)
        {
            modSelect.Hide();

            base.OnSuspending(next);
        }

        protected override bool OnExiting(Screen next)
        {
            if (modSelect.State == Visibility.Visible)
            {
                modSelect.Hide();
                return true;
            }

            if (base.OnExiting(next))
                return true;

            if (Beatmap.Value.Track != null)
                Beatmap.Value.Track.Looping = false;

            return false;
        }

        protected override bool OnKeyDown(InputState state, KeyDownEventArgs args)
        {
            controlPressed = state.Keyboard.ControlPressed;
            return base.OnKeyDown(state, args);
        }

        protected override bool OnKeyUp(InputState state, KeyUpEventArgs args)
        {
            controlPressed = state.Keyboard.ControlPressed;
            return base.OnKeyUp(state, args);
        }

        protected override void OnSelected()
        {
            if (player != null) return;

            originalMods = modSelect.SelectedMods.Value;
            if (controlPressed)
                if (findAutoMod(originalMods) == null)
                {
                    var auto = findAutoMod(Ruleset.Value.CreateInstance().GetModsFor(ModType.Special));
                    if (auto != null)
                        modSelect.SelectedMods.Value = originalMods.Concat(new[] { auto });
                }

            Beatmap.Value.Track.Looping = false;
            Beatmap.Disabled = true;

            LoadComponentAsync(player = new PlayerLoader(new Player()), l => Push(player));
        }

        private Mod findAutoMod(IEnumerable<Mod> mods)
        {
            foreach (var mod in mods)
            {
                if (mod is ModAutoplay) return mod;
                var multimod = mod as MultiMod;
                if (multimod != null)
                {
                    var find = findAutoMod(multimod.Mods);
                    if (find != null) return find;
                }
            }
            return null;
        }
    }
}
