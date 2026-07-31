public enum BuildStructureType // 건축 구조 역할
{
    None = 0, // 구조 연결 미사용
    Foundation = 1, // 지형 위 기초
    Floor = 2, // 기초 위 바닥
    Wall = 3, // 기초 또는 바닥 위 벽
    Furniture = 4, // 기능성 가구
    Roof = 5 // 지붕 구조
}