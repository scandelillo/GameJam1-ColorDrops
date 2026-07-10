// IColorEntity.cs
// Cualquier cosa que tenga un color de combate implementa esto.
// Gracias a esta interfaz, el sistema de combate no necesita saber
// si está pegando a una gota o al jugador: solo pregunta "¿de qué color eres?"
public interface IColorEntity
{
    DropletColor DColor { get; }
}