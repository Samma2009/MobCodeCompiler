namespace example1
{
    class classA
    {
        function functionA
        {
            tellraw @a "hi from classA/functionA";
        }

        function functionB
        {
            tellraw @a "hi from classA/functionB";
        }
    }
    
    class classB
    {
        function functionA
        {
            tellraw @a "hi from classB/functionA";
        }

        function functionB
        {
            tellraw @a "hi from classB/functionB";
        }
    }

    function classless
    {
        tellraw @a "hi from classless";
    }
}