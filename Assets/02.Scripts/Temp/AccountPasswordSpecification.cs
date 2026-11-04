using System.Text.RegularExpressions;

public class AccountPasswordSpecification
{
    // 비밀번호: 공백 없는 8~30글자, 대문자·소문자·숫자·기호(영문·숫자가 아닌 문자)를 각각 1개 이상 포함
    // 예) Abcd1234!, Zomboid#2026
    private static readonly Regex PasswordRegex = new Regex(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*[0-9])(?=.*[^A-Za-z0-9\s])\S{8,30}$");

    public string ErrorMessage => "비밀번호는 대문자, 소문자, 숫자, 기호를 모두 포함한 8~30글자여야 합니다.";

    public bool IsSatisfiedBy(string password)
    {
        return PasswordRegex.IsMatch(password);
    }
}
