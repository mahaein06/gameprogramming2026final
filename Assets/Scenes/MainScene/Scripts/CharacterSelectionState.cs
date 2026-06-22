public enum SelectableAnimal
{
    Deer,
    Horse,
    Penguin,
    Dog,
    Tiger
}

public static class CharacterSelectionState
{
    public static SelectableAnimal SelectedAnimal { get; private set; } = SelectableAnimal.Dog;

    public static void Select(SelectableAnimal animal)
    {
        SelectedAnimal = animal;
    }
}
