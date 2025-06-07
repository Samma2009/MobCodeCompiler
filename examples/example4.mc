namespace example4
{
    @load
    function load
    {
        say load;
    }

    @tick
    function tick
    {
        playsound minecraft:entity.arrow.hit_player voice @a;
    }
}