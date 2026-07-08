using UnityEngine;

// Clase estática: no se adjunta a ningún GameObject, solo se llama desde otros scripts
// como ColorEffectiveness.GetMultiplier(...)
public static class ColorEffectiveness
{
    // Multiplicadores de daño según la relación Pokémon-style
    private const float SuperEffective = 2f;
    private const float NotVeryEffective = 0.5f;
    private const float Neutral = 1f; // Por si en el futuro agregan más colores sin relación directa

    // Devuelve cuánto multiplicador de daño aplica cuando "attackerColor" golpea a "defenderColor"
    public static float GetMultiplier(DropletColor attackerColor, DropletColor defenderColor)
    {
        // Mismo color: no debería llegar a llamarse esto (se filtra antes),
        // pero por seguridad devolvemos 0 daño
        if (attackerColor == defenderColor)
        {
            return 0f;
        }

        // Triángulo: Rojo > Verde > Azul > Rojo
        bool isSuperEffective =
            (attackerColor == DropletColor.Red && defenderColor == DropletColor.Green) ||
            (attackerColor == DropletColor.Green && defenderColor == DropletColor.Blue) ||
            (attackerColor == DropletColor.Blue && defenderColor == DropletColor.Red);

        if (isSuperEffective)
        {
            return SuperEffective;
        }

        // Si no es súper efectivo y no son el mismo color, es la relación inversa (poco efectivo)
        return NotVeryEffective;
    }
}