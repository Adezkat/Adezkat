using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace AshenAnvil.EditorTools
{
    /// <summary>
    /// Authors the shared humanoid-ish animation clips (keyframed on the procedural rig's bone
    /// paths) and the Animator controller used by every hero and monster.
    /// </summary>
    public static class AnimationBuilder
    {
        public static RuntimeAnimatorController Build()
        {
            GeneratorPaths.Ensure(GeneratorPaths.Animations);

            var idle = Save(Idle(), true);
            var run = Save(Run(), true);
            var attack = Save(Attack(), false);
            var cast = Save(Cast(), false);
            var hit = Save(Hit(), false);
            var die = Save(Die(), false);

            string path = GeneratorPaths.Animations + "/CharacterAnimator.controller";
            AssetDatabase.DeleteAsset(path);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Cast", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Dead", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("AttackSpeed", AnimatorControllerParameterType.Float);
            var parameters = ctrl.parameters;
            foreach (var p in parameters)
                if (p.name == "AttackSpeed") p.defaultFloat = 1f;
            ctrl.parameters = parameters;

            var sm = ctrl.layers[0].stateMachine;
            var loco = ctrl.CreateBlendTreeInController("Locomotion", out var tree, 0);
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            tree.AddChild(idle, 0f);
            tree.AddChild(run, 1f);
            sm.defaultState = loco;

            var sAttack = sm.AddState("Attack", new Vector3(450, 0));
            sAttack.motion = attack;
            sAttack.speedParameterActive = true;
            sAttack.speedParameter = "AttackSpeed";
            var sCast = sm.AddState("Cast", new Vector3(450, 80));
            sCast.motion = cast;
            var sHit = sm.AddState("Hit", new Vector3(450, 160));
            sHit.motion = hit;
            var sDie = sm.AddState("Die", new Vector3(450, 260));
            sDie.motion = die;

            AnyTo(sm, sAttack, "Attack", 0.05f, true);
            AnyTo(sm, sCast, "Cast", 0.06f, true);
            AnyTo(sm, sHit, "Hit", 0.04f, false);
            Back(sAttack, loco, 0.85f);
            Back(sCast, loco, 0.85f);
            Back(sHit, loco, 0.8f);

            var toDie = sm.AddAnyStateTransition(sDie);
            toDie.AddCondition(AnimatorConditionMode.If, 0, "Dead");
            toDie.canTransitionToSelf = false;
            toDie.duration = 0.08f;
            var revive = sDie.AddTransition(loco);
            revive.AddCondition(AnimatorConditionMode.IfNot, 0, "Dead");
            revive.hasExitTime = false;
            revive.duration = 0.2f;

            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            return ctrl;
        }

        static void AnyTo(AnimatorStateMachine sm, AnimatorState state, string trigger, float duration, bool self)
        {
            var t = sm.AddAnyStateTransition(state);
            t.AddCondition(AnimatorConditionMode.If, 0, trigger);
            t.AddCondition(AnimatorConditionMode.IfNot, 0, "Dead");
            t.duration = duration;
            t.hasExitTime = false;
            t.canTransitionToSelf = self;
        }

        static void Back(AnimatorState from, AnimatorState to, float exit)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = true;
            t.exitTime = exit;
            t.duration = 0.12f;
        }

        static AnimationClip Save(AnimationClip clip, bool loop)
        {
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            clip.EnsureQuaternionContinuity();
            string path = $"{GeneratorPaths.Animations}/{clip.name}.anim";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        // ------------------------------------------------------------------ clip authoring
        struct K
        {
            public float t; public Vector3 v;
            public K(float t, float x, float y, float z) { this.t = t; v = new Vector3(x, y, z); }
        }

        static void Rot(AnimationClip clip, string bone, params K[] keys)
        {
            var cx = new AnimationCurve(); var cy = new AnimationCurve(); var cz = new AnimationCurve(); var cw = new AnimationCurve();
            Quaternion prev = Quaternion.identity;
            foreach (var k in keys)
            {
                var q = Quaternion.Euler(k.v);
                if (Quaternion.Dot(prev, q) < 0f) q = new Quaternion(-q.x, -q.y, -q.z, -q.w);
                prev = q;
                cx.AddKey(k.t, q.x); cy.AddKey(k.t, q.y); cz.AddKey(k.t, q.z); cw.AddKey(k.t, q.w);
            }
            clip.SetCurve(bone, typeof(Transform), "localRotation.x", cx);
            clip.SetCurve(bone, typeof(Transform), "localRotation.y", cy);
            clip.SetCurve(bone, typeof(Transform), "localRotation.z", cz);
            clip.SetCurve(bone, typeof(Transform), "localRotation.w", cw);
        }

        static void Pos(AnimationClip clip, string bone, params K[] keys)
        {
            var cx = new AnimationCurve(); var cy = new AnimationCurve(); var cz = new AnimationCurve();
            foreach (var k in keys) { cx.AddKey(k.t, k.v.x); cy.AddKey(k.t, k.v.y); cz.AddKey(k.t, k.v.z); }
            clip.SetCurve(bone, typeof(Transform), "localPosition.x", cx);
            clip.SetCurve(bone, typeof(Transform), "localPosition.y", cy);
            clip.SetCurve(bone, typeof(Transform), "localPosition.z", cz);
        }

        static AnimationClip NewClip(string name) => new AnimationClip { name = name, frameRate = 30 };
        const float H = CharacterRig.HipHeight;

        /// <summary>Resets every bone not animated by a clip so states blend predictably.</summary>
        static void Rest(AnimationClip c, float end, params string[] bones)
        {
            foreach (var b in bones) Rot(c, b, new K(0, 0, 0, 0), new K(end, 0, 0, 0));
        }

        static AnimationClip Idle()
        {
            var c = NewClip("Idle");
            Pos(c, CharacterRig.Hips, new K(0, 0, H, 0), new K(1f, 0, H + 0.025f, 0), new K(2f, 0, H, 0));
            Rot(c, CharacterRig.Spine, new K(0, 0, 0, 0), new K(1f, 3, 0, 0), new K(2f, 0, 0, 0));
            Rot(c, CharacterRig.Head, new K(0, 0, 0, 0), new K(1f, -4, 4, 0), new K(2f, 0, 0, 0));
            Rot(c, CharacterRig.ArmL, new K(0, 0, 0, -6), new K(1f, -4, 0, -10), new K(2f, 0, 0, -6));
            Rot(c, CharacterRig.ArmR, new K(0, -10, 0, 6), new K(1f, -14, 0, 10), new K(2f, -10, 0, 6));
            Rest(c, 2f, CharacterRig.LegL, CharacterRig.LegR, CharacterRig.Hips);
            return c;
        }

        static AnimationClip Run()
        {
            var c = NewClip("Run");
            const float T = 0.6f;
            Pos(c, CharacterRig.Hips, new K(0, 0, H, 0), new K(T * 0.25f, 0, H + 0.07f, 0), new K(T * 0.5f, 0, H, 0), new K(T * 0.75f, 0, H + 0.07f, 0), new K(T, 0, H, 0));
            Rot(c, CharacterRig.LegL, new K(0, -38, 0, 0), new K(T * 0.5f, 38, 0, 0), new K(T, -38, 0, 0));
            Rot(c, CharacterRig.LegR, new K(0, 38, 0, 0), new K(T * 0.5f, -38, 0, 0), new K(T, 38, 0, 0));
            Rot(c, CharacterRig.ArmL, new K(0, 32, 0, -8), new K(T * 0.5f, -32, 0, -8), new K(T, 32, 0, -8));
            Rot(c, CharacterRig.ArmR, new K(0, -40, 0, 8), new K(T * 0.5f, 20, 0, 8), new K(T, -40, 0, 8));
            Rot(c, CharacterRig.Spine, new K(0, 10, 4, 0), new K(T * 0.5f, 10, -4, 0), new K(T, 10, 4, 0));
            Rot(c, CharacterRig.Head, new K(0, -8, 0, 0), new K(T, -8, 0, 0));
            Rest(c, T, CharacterRig.Hips);
            return c;
        }

        static AnimationClip Attack()
        {
            var c = NewClip("Attack");
            const float T = 0.5f;
            Rot(c, CharacterRig.ArmR, new K(0, -10, 0, 6), new K(0.14f, -170, -10, 10), new K(0.28f, -35, 20, 0), new K(T, -10, 0, 6));
            Rot(c, CharacterRig.ArmL, new K(0, 0, 0, -6), new K(0.14f, -30, 0, -20), new K(0.28f, 20, 0, -10), new K(T, 0, 0, -6));
            Rot(c, CharacterRig.Spine, new K(0, 0, 0, 0), new K(0.14f, -6, -25, 0), new K(0.28f, 12, 30, 0), new K(T, 0, 0, 0));
            Rot(c, CharacterRig.LegL, new K(0, 0, 0, 0), new K(0.2f, -25, 0, 0), new K(T, 0, 0, 0));
            Rot(c, CharacterRig.LegR, new K(0, 0, 0, 0), new K(0.2f, 15, 0, 0), new K(T, 0, 0, 0));
            Pos(c, CharacterRig.Hips, new K(0, 0, H, 0), new K(0.25f, 0, H - 0.06f, 0.05f), new K(T, 0, H, 0));
            Rest(c, T, CharacterRig.Head, CharacterRig.Hips);
            return c;
        }

        static AnimationClip Cast()
        {
            var c = NewClip("Cast");
            const float T = 0.6f;
            Rot(c, CharacterRig.ArmR, new K(0, -10, 0, 6), new K(0.25f, -160, 0, 20), new K(0.4f, -90, 0, 5), new K(T, -10, 0, 6));
            Rot(c, CharacterRig.ArmL, new K(0, 0, 0, -6), new K(0.25f, -160, 0, -20), new K(0.4f, -90, 0, -5), new K(T, 0, 0, -6));
            Rot(c, CharacterRig.Spine, new K(0, 0, 0, 0), new K(0.25f, -12, 0, 0), new K(0.4f, 14, 0, 0), new K(T, 0, 0, 0));
            Rot(c, CharacterRig.Head, new K(0, 0, 0, 0), new K(0.25f, -15, 0, 0), new K(T, 0, 0, 0));
            Pos(c, CharacterRig.Hips, new K(0, 0, H, 0), new K(0.25f, 0, H + 0.05f, 0), new K(0.4f, 0, H - 0.08f, 0), new K(T, 0, H, 0));
            Rest(c, T, CharacterRig.LegL, CharacterRig.LegR, CharacterRig.Hips);
            return c;
        }

        static AnimationClip Hit()
        {
            var c = NewClip("Hit");
            const float T = 0.3f;
            Rot(c, CharacterRig.Spine, new K(0, 0, 0, 0), new K(0.08f, -22, 0, 4), new K(T, 0, 0, 0));
            Rot(c, CharacterRig.Head, new K(0, 0, 0, 0), new K(0.08f, -18, 0, 0), new K(T, 0, 0, 0));
            Rot(c, CharacterRig.ArmL, new K(0, 0, 0, -6), new K(0.08f, 20, 0, -30), new K(T, 0, 0, -6));
            Rot(c, CharacterRig.ArmR, new K(0, -10, 0, 6), new K(0.08f, 20, 0, 30), new K(T, -10, 0, 6));
            Rest(c, T, CharacterRig.LegL, CharacterRig.LegR, CharacterRig.Hips);
            Pos(c, CharacterRig.Hips, new K(0, 0, H, 0), new K(0.08f, 0, H, -0.08f), new K(T, 0, H, 0));
            return c;
        }

        static AnimationClip Die()
        {
            var c = NewClip("Die");
            const float T = 0.7f;
            Rot(c, CharacterRig.Hips, new K(0, 0, 0, 0), new K(0.2f, -20, 0, 0), new K(0.55f, -88, 0, 0), new K(T, -84, 0, 0));
            Pos(c, CharacterRig.Hips, new K(0, 0, H, 0), new K(0.3f, 0, H * 0.8f, -0.1f), new K(0.55f, 0, 0.22f, -0.4f), new K(T, 0, 0.22f, -0.4f));
            Rot(c, CharacterRig.ArmL, new K(0, 0, 0, -6), new K(0.55f, -40, 0, -70), new K(T, -40, 0, -75));
            Rot(c, CharacterRig.ArmR, new K(0, -10, 0, 6), new K(0.55f, -40, 0, 70), new K(T, -40, 0, 75));
            Rot(c, CharacterRig.LegL, new K(0, 0, 0, 0), new K(0.55f, -20, 0, -8), new K(T, -18, 0, -8));
            Rot(c, CharacterRig.LegR, new K(0, 0, 0, 0), new K(0.55f, -5, 0, 8), new K(T, -6, 0, 8));
            Rot(c, CharacterRig.Head, new K(0, 0, 0, 0), new K(0.55f, 20, 15, 0), new K(T, 15, 20, 0));
            Rest(c, T, CharacterRig.Spine);
            return c;
        }
    }
}
