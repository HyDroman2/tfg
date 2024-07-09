public abstract class DecisionTreeNode
{
    public abstract DecisionTreeNode makeDecision();
}



public class Decision : DecisionTreeNode
{
    private DecisionTreeNode trueNode;
    private DecisionTreeNode falseNode;
    private System.Func<bool> testValue;

    public Decision(DecisionTreeNode trueNode, DecisionTreeNode falseNode, System.Func<bool> testValue)
    {
        this.trueNode = trueNode;
        this.falseNode = falseNode;
        this.testValue = testValue;

    }
    public DecisionTreeNode getBranch()
    {
        if (testValue())
            return trueNode;
        else
            return falseNode;
    }

    public override DecisionTreeNode makeDecision()
    {
        return getBranch().makeDecision();
    }

}

public class MultiDecision : DecisionTreeNode
{
    private DecisionTreeNode[] options;
    private System.Func<int> testValue;

    public MultiDecision(DecisionTreeNode[] options, System.Func<int> testValue)
    {
        this.options = options;
        this.testValue = testValue;

    }

    public override DecisionTreeNode makeDecision()
    {
        return options[testValue()];
    }
}

public class TreeNodeAction : DecisionTreeNode
{

    public System.Func<Action> accion;

    public TreeNodeAction(System.Func<Action> accion)
    {
        this.accion = accion;
    }
    public override DecisionTreeNode makeDecision()
    {
        return this;
    }
}

