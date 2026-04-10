//오브젝트가 꺼지고 켜지는 것을 이용함
public interface IOnOff
{
    public bool Active { get; set; }
    public bool On();

    public bool Off();
}
