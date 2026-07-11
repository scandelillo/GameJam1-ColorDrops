// Reglas del triángulo de color: Rojo > Amarillo > Azul > Rojo.
// Clase estática: no se pone en ningún GameObject, solo se llama desde el combate.
public static class ColorRules
{
    // Multiplicadores de daño según la relación de colores.
    private const float Fuerte = 2f;   // súper efectivo (gana el triángulo)
    private const float Debil = 0.5f;  // poco efectivo (pierde el triángulo)
    private const float Nulo = 0f;     // mismo color: no hace daño

    // Devuelve el multiplicador de daño cuando "atacante" golpea a "defensor".
    public static float Multiplicador(DropletColor atacante, DropletColor defensor)
    {
        // Mismo color: no se hace daño.
        if (atacante == defensor)
            return Nulo;

        // Relaciones donde el atacante es fuerte.
        bool esFuerte =
            (atacante == DropletColor.Rojo && defensor == DropletColor.Amarillo) ||
            (atacante == DropletColor.Amarillo && defensor == DropletColor.Azul) ||
            (atacante == DropletColor.Azul && defensor == DropletColor.Rojo);

        // Si no es fuerte (y no es el mismo color), es la relación inversa: débil.
        return esFuerte ? Fuerte : Debil;
    }
}
