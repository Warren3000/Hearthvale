public class Weapon
{
    public string Name { get; }
    public int Level { get; private set; }
    public int XP { get; private set; }
    public int Damage { get; private set; }

    public Weapon(string name, int baseDamage)
    {
        Name = name;
        Level = 1;
        XP = 0;
        Damage = baseDamage;
    }

    public void GainXP(int amount)
    {
        XP += amount;
        if (XP >= XPToNextLevel())
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        Level++;
        XP = 0;
        Damage += 2; // Example increment
    }

    private int XPToNextLevel() => Level * 10;
}