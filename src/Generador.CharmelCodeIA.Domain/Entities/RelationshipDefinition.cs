namespace Generador.CharmelCodeIA.Domain.Entities;

public class RelationshipDefinition
{
    public string Name { get; set; } = string.Empty;
    public string PrincipalTable { get; set; } = string.Empty;
    public string PrincipalSchema { get; set; } = string.Empty;
    public string PrincipalColumn { get; set; } = string.Empty;
    public string DependentTable { get; set; } = string.Empty;
    public string DependentSchema { get; set; } = string.Empty;
    public string DependentColumn { get; set; } = string.Empty;
    public RelationshipType Type { get; set; }
    public bool IsRequired { get; set; }
    public DeleteBehavior DeleteBehavior { get; set; } = DeleteBehavior.NoAction;
}

public enum RelationshipType
{
    OneToOne = 1,
    OneToMany = 2,
    ManyToMany = 3
}

public enum DeleteBehavior
{
    NoAction = 0,
    Cascade = 1,
    SetNull = 2,
    Restrict = 3
}
