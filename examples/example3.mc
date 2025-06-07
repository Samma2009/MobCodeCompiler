namespace example3
{
    function calculate
    {
        $number = 10 * 2 - 5;
        say $number$;
    }

    function if
    {
        $a = 5;
        $b = 3;

        if $a$ > $b$
        {
            say true;
        }
    }

    function expif
    {
        $cond = {"a":5,"b":3};

        if $cond.a$ > $cond.b$
        {
            say true;
        }
    }

    function times
    {
        $repeat = 3;

        times $repeat$
        {
            say repeated;
        }
    }

    function while
    {
        $countdown = 10;

        while $countdown$ > 0
        {
            $countdown = $countdown$ - 1;
            say repeated;
        }
    }
}