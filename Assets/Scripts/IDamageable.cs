// IDamageable.cs
// Cualquier cosa que pueda recibir daño implementa esto (jugador, gotas, etc.)
public interface IDamageable
{
    void TakeDamage(float amount);
}