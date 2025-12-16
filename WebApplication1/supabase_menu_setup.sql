-- ============================================
-- Supabase 메뉴 테이블 생성 스크립트
-- ============================================
-- 실행 방법:
-- 1. Supabase 대시보드 (https://supabase.com/dashboard) 접속
-- 2. 프로젝트 선택
-- 3. 왼쪽 메뉴에서 "SQL Editor" 클릭
-- 4. 아래 SQL을 복사하여 붙여넣기
-- 5. "Run" 버튼 클릭
-- ============================================

-- 기존 테이블이 있으면 삭제 (선택사항 - 처음 생성할 때만)
-- DROP TABLE IF EXISTS menus CASCADE;

-- menus 테이블 생성
CREATE TABLE IF NOT EXISTS menus (
    id SERIAL PRIMARY KEY,
    name TEXT NOT NULL,
    url TEXT,
    controller TEXT,
    action TEXT,
    icon TEXT,
    display_order INTEGER NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT true,
    parent_id INTEGER REFERENCES menus(id) ON DELETE CASCADE,
    level INTEGER NOT NULL CHECK (level IN (1, 2, 3)),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- 테이블 코멘트 추가
COMMENT ON TABLE menus IS '3단계 메뉴 시스템 테이블';
COMMENT ON COLUMN menus.id IS '메뉴 고유 ID';
COMMENT ON COLUMN menus.name IS '메뉴 표시 이름';
COMMENT ON COLUMN menus.url IS '직접 URL (controller/action 대신 사용 가능)';
COMMENT ON COLUMN menus.controller IS 'ASP.NET Core 컨트롤러 이름';
COMMENT ON COLUMN menus.action IS 'ASP.NET Core 액션 이름';
COMMENT ON COLUMN menus.icon IS '아이콘 CSS 클래스 (예: bi bi-house)';
COMMENT ON COLUMN menus.display_order IS '표시 순서 (작을수록 먼저 표시)';
COMMENT ON COLUMN menus.is_active IS '활성화 여부';
COMMENT ON COLUMN menus.parent_id IS '부모 메뉴 ID (NULL이면 1단계 메뉴)';
COMMENT ON COLUMN menus.level IS '메뉴 깊이 레벨 (1, 2, 3)';
COMMENT ON COLUMN menus.created_at IS '생성일시';
COMMENT ON COLUMN menus.updated_at IS '수정일시';

-- 인덱스 추가 (성능 향상)
CREATE INDEX IF NOT EXISTS idx_menus_parent_id ON menus(parent_id);
CREATE INDEX IF NOT EXISTS idx_menus_level ON menus(level);
CREATE INDEX IF NOT EXISTS idx_menus_display_order ON menus(display_order);
CREATE INDEX IF NOT EXISTS idx_menus_is_active ON menus(is_active);

-- updated_at 자동 업데이트 함수 생성
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ language 'plpgsql';

-- updated_at 자동 업데이트 트리거 생성
DROP TRIGGER IF EXISTS update_menus_updated_at ON menus;
CREATE TRIGGER update_menus_updated_at
    BEFORE UPDATE ON menus
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Row Level Security (RLS) 설정
ALTER TABLE menus ENABLE ROW LEVEL SECURITY;

-- 개발용 정책: 모든 사용자가 읽기/쓰기 가능 (개발 환경용)
-- 프로덕션에서는 인증된 사용자만 접근하도록 변경하세요.
DROP POLICY IF EXISTS "Allow all operations for development" ON menus;
CREATE POLICY "Allow all operations for development" 
ON menus FOR ALL 
USING (true) 
WITH CHECK (true);

-- 체크 제약조건: level과 parent_id의 일관성 검증
-- 1단계 메뉴는 parent_id가 NULL이어야 함
-- 2, 3단계 메뉴는 parent_id가 NOT NULL이어야 함
ALTER TABLE menus DROP CONSTRAINT IF EXISTS check_menu_level_parent;
ALTER TABLE menus ADD CONSTRAINT check_menu_level_parent 
    CHECK (
        (level = 1 AND parent_id IS NULL) OR 
        (level IN (2, 3) AND parent_id IS NOT NULL)
    );

-- 체크 제약조건: parent_id가 자기 자신을 참조하지 않도록
ALTER TABLE menus DROP CONSTRAINT IF EXISTS check_menu_no_self_reference;
ALTER TABLE menus ADD CONSTRAINT check_menu_no_self_reference 
    CHECK (id != parent_id);

-- 테이블 생성 확인
SELECT 'menus 테이블이 성공적으로 생성되었습니다!' AS message;
SELECT * FROM menus LIMIT 0;

-- 샘플 데이터 삽입 (선택사항)
-- 아래 주석을 해제하여 샘플 메뉴를 추가할 수 있습니다.

/*
-- 1단계 메뉴
INSERT INTO menus (name, controller, action, display_order, level, is_active) VALUES
('홈', 'Home', 'Index', 1, 1, true),
('게시물', 'Post', 'Index', 2, 1, true),
('메뉴 관리', 'Menu', 'Index', 3, 1, true);

-- 2단계 메뉴 (게시물 하위)
INSERT INTO menus (name, controller, action, display_order, level, parent_id, is_active) 
SELECT 
    '게시물 목록', 'Post', 'Index', 1, 2, id, true
FROM menus WHERE name = '게시물' AND level = 1;

INSERT INTO menus (name, controller, action, display_order, level, parent_id, is_active) 
SELECT 
    '게시물 작성', 'Post', 'Create', 2, 2, id, true
FROM menus WHERE name = '게시물' AND level = 1;

-- 3단계 메뉴 예시 (필요시 사용)
-- INSERT INTO menus (name, url, display_order, level, parent_id, is_active) 
-- SELECT 
--     '서브 메뉴 1', '/example/sub1', 1, 3, id, true
-- FROM menus WHERE name = '게시물 목록' AND level = 2;
*/

