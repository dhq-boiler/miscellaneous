

IEnumerable<int> MethodForYield()
{
    yield return 1;
    yield return 2;
    yield return 3;
    yield break;
    yield return 4;
    yield return 5;
    yield return 6;
}

foreach (var intVal in MethodForYield())
{
    Console.WriteLine(intVal);
}

