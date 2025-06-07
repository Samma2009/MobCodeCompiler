namespace example2
{
    function survival
    {
        Player.Gamemode(@s,survival);
    }

    function creative
    {
        Player.Gamemode(@s,creative);
    }

    function stick
    {
        Item.Give(@s,minecraft:stick);
    }
}