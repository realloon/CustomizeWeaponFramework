using Verse;

namespace CWF;

public class CompRenamable : ThingComp {
    private string _nickname;

    public string Nickname {
        get => _nickname;
        set {
            if (!string.IsNullOrEmpty(value)) _nickname = value;

            var artComp = parent.TryGetComp<RimWorld.CompArt>();
            if (artComp != null) {
                artComp.Title = value;
            }
        }
    }

    public override string TransformLabel(string label) {
        return !Nickname.NullOrEmpty() ? Nickname : label;
    }

    public override void PostExposeData() {
        base.PostExposeData();
        Scribe_Values.Look(ref _nickname, "nickname");
    }

    // debug
    public override IEnumerable<Gizmo> CompGetGizmosExtra() {
        foreach (var g in base.CompGetGizmosExtra())
            yield return g;

        if (!Prefs.DevMode) yield break;

        yield return new Command_Action {
            defaultLabel = "Dev: Rename",
            action = () => { Nickname = "hello world"; }
        };
    }
}