using System.Runtime.CompilerServices;

// Assets/Editor/Tests의 테스트가 internal 멤버(Teleport.TryBegin 등)를 호출하기 위해 필요하다.
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor")]
