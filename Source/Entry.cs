// Independent recovery/compatibility add-on for DefectSkinMod v1.0 on STS2 0.113.x.
// The original author's DLL and PCK are left unchanged and are not redistributed.
// WARNING: the original initializer will still log an error before this mod loads.
using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace DefectSkinModFix;

[ModInitializer(nameof(Init))]
public sealed class Entry
{
    private const string BadPatchClass =
        "DefectSkinMod.Scripts.NCharacterSelectScreen_SelectCharacter_Patch";
    private const string GlobalsClass = "DefectSkinMod.Scripts.DefectSkinGlobals";
    private static readonly Harmony PatchEngine = new("community.defectskinmod.fix.v0113");
    private static MethodInfo? _originalPostfix;

    public static void Init()
    {
        Console.WriteLine("[DefectSkinModFix] Initializing independent compatibility patch.");

        var originalAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "DefectSkinMod");
        if (originalAssembly is null)
        {
            Console.WriteLine("[DefectSkinModFix] ERROR: Original DefectSkinMod assembly not loaded. Enable it first.");
            return;
        }

        var oldSelectPatchType = originalAssembly.GetType(BadPatchClass);
        if (oldSelectPatchType is null)
        {
            Console.WriteLine("[DefectSkinModFix] ERROR: Original SelectCharacter patch class not found; unsupported mod version.");
            return;
        }

        // Avoid installing a second equivalent patch if the original author
        // has already fixed the mod or an earlier compatibility patch is active.
        if (IsOriginalPatchInstalled(oldSelectPatchType))
        {
            Console.WriteLine("[DefectSkinModFix] Original SelectCharacter patch is active; not patching it twice.");
            return;
        }

        _originalPostfix = oldSelectPatchType.GetMethod("Postfix",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        if (_originalPostfix is null || _originalPostfix.GetParameters().Length != 3)
        {
            Console.WriteLine("[DefectSkinModFix] ERROR: Unexpected original Postfix signature; refusing to guess.");
            return;
        }

        // The original Entry.Init() calls Harmony.PatchAll() followed by
        // DefectSkinGlobals.Preload(). PatchAll throws on the old 3-argument
        // SelectCharacter Postfix, so Preload never runs. Recover missing
        // original patches selectively before invoking Preload below.
        int recovered = 0;
        int failures = 0;
        foreach (var patchType in GetTypesSafely(originalAssembly)
                     .Where(t => t.Name.EndsWith("_Patch", StringComparison.Ordinal)
                              && t.FullName != BadPatchClass))
        {
            if (IsOriginalPatchInstalled(patchType))
                continue;
            try
            {
                PatchEngine.CreateClassProcessor(patchType).Patch();
                ++recovered;
                Console.WriteLine($"[DefectSkinModFix] Recovered original patch: {patchType.Name}");
            }
            catch (Exception e)
            {
                ++failures;
                Console.WriteLine($"[DefectSkinModFix] WARNING: Failed to recover {patchType.Name}: {e}");
            }
        }

        try
        {
            var method = AccessTools.Method(typeof(NCharacterSelectScreen),
                "SelectCharacter", new[] { typeof(NCharacterSelectButton) });
            if (method is null)
                throw new MissingMethodException("NCharacterSelectScreen.SelectCharacter(NCharacterSelectButton)");

            var replacement = AccessTools.Method(typeof(Entry), nameof(SelectCharacterPostfix));
            if (replacement is null)
                throw new MissingMethodException(nameof(SelectCharacterPostfix));

            PatchEngine.Patch(method, postfix: new HarmonyMethod(replacement));
            Console.WriteLine("[DefectSkinModFix] Registered v0.113 SelectCharacter adapter.");
        }
        catch (Exception e)
        {
            Console.WriteLine($"[DefectSkinModFix] ERROR: Could not register adapter: {e}");
            return;
        }

        try
        {
            var preload = originalAssembly.GetType(GlobalsClass)?.GetMethod("Preload",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (preload is null)
                throw new MissingMethodException(GlobalsClass, "Preload");
            preload.Invoke(null, null);
            Console.WriteLine("[DefectSkinModFix] Executed skipped original asset preload.");
        }
        catch (Exception e)
        {
            ++failures;
            Console.WriteLine($"[DefectSkinModFix] ERROR: Original asset preload failed: {e}");
        }

        Console.WriteLine($"[DefectSkinModFix] Recovery finished: {recovered} patch class(es) applied, {failures} issue(s). " +
                          "Check model in character select, combat, merchant and rest-site.");
    }

    // Harmony binds the v0.113 argument charSelectButton. The now-removed
    // original CharacterModel argument is reconstructed from its Character property.
    // This wrapper calls the original author's unmodified skin implementation.
    private static void SelectCharacterPostfix(
        NCharacterSelectScreen __instance, NCharacterSelectButton charSelectButton)
    {
        var original = _originalPostfix;
        if (original is null)
            return;

        try
        {
            // Use reflection only for the changing game-side property surface.
            // A missing Character property is logged instead of fabricating a model.
            var property = AccessTools.Property(charSelectButton.GetType(), "Character");
            if (property is null)
                throw new MissingMemberException(charSelectButton.GetType().FullName, "Character");
            var characterModel = property.GetValue(charSelectButton);
            original.Invoke(null, new object?[] { __instance, charSelectButton, characterModel });
        }
        catch (TargetInvocationException ex)
        {
            Console.WriteLine($"[DefectSkinModFix] ERROR in original skin Postfix: {ex.InnerException ?? ex}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DefectSkinModFix] ERROR in SelectCharacter adapter: {ex}");
        }
    }

    private static Type[] GetTypesSafely(Assembly assembly)
    {
        try { return assembly.GetTypes(); }
        catch (ReflectionTypeLoadException ex)
        {
            foreach (var e in ex.LoaderExceptions.Where(e => e is not null))
                Console.WriteLine($"[DefectSkinModFix] WARNING loading original patch types: {e}");
            return ex.Types.Where(t => t is not null).Cast<Type>().ToArray();
        }
    }

    private static bool IsOriginalPatchInstalled(Type originalPatchType)
    {
        try
        {
            foreach (var method in Harmony.GetAllPatchedMethods())
            {
                var info = Harmony.GetPatchInfo(method);
                if (info is null) continue;
                if (info.Prefixes.Concat(info.Postfixes)
                    .Concat(info.Transpilers).Concat(info.Finalizers)
                    .Any(p => p.PatchMethod.DeclaringType == originalPatchType))
                    return true;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"[DefectSkinModFix] WARNING: Patch inventory check failed: {e.Message}");
        }
        return false;
    }
}
