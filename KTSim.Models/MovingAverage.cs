namespace KTSim.Models;

/* Tensors structure
    * 
    * AIState
    *  - OperativeStates
    *      - 0 PositionX
    *      - 1 PositionY
    *      - 2 HasMoved
    *      - 3 HasDashed
    *      - 4 HasShot
    *      - 5 Ready
    *      - 6 Active
    *      - 7 Activated
    *      - 8 Neutralized
    * 
    * AIAction
    *  - OperativeActions
    *      - 0 ShootReward 
    *      - 1 Target
    *      - 2 MoveReward
    *      - 3 MoveX
    *      - 4 MoveY
    *      - 5 DashReward
    *      - 6 DashX
    *      - 7 DashY
*/

public class MovingAverage
{
    private List<float> _values = [];

    int _maxSize;

    public MovingAverage(int maxSize)
    {
        _maxSize = maxSize;
    }

    public void Add(float value)
    {
        if (_values.Count >= _maxSize)
            _values.RemoveAt(0);

        _values.Add(value);

    }

    public float Average()
    {
        if (_values.Count == 0)
            return 0;

        return _values.Average();
    }
}
