-- ============================================
-- Supabase 데이터베이스 테이블 생성 스크립트
-- ============================================
-- 실행 방법:
-- 1. Supabase 대시보드 (https://supabase.com/dashboard) 접속
-- 2. 프로젝트 선택
-- 3. 왼쪽 메뉴에서 "SQL Editor" 클릭
-- 4. 아래 SQL을 복사하여 붙여넣기
-- 5. "Run" 버튼 클릭
-- ============================================

-- 기존 테이블이 있으면 삭제 (선택사항 - 처음 생성할 때만)
-- DROP TABLE IF EXISTS posts CASCADE;

-- posts 테이블 생성
CREATE TABLE IF NOT EXISTS posts (
    id SERIAL PRIMARY KEY,
    title TEXT NOT NULL,
    content TEXT NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- 테이블 코멘트 추가
COMMENT ON TABLE posts IS '게시물 테이블';
COMMENT ON COLUMN posts.id IS '게시물 고유 ID';
COMMENT ON COLUMN posts.title IS '게시물 제목';
COMMENT ON COLUMN posts.content IS '게시물 내용';
COMMENT ON COLUMN posts.created_at IS '생성일시';

-- 인덱스 추가 (성능 향상)
CREATE INDEX IF NOT EXISTS idx_posts_created_at ON posts(created_at DESC);

-- Row Level Security (RLS) 설정
-- 개발 환경에서는 RLS를 비활성화하여 모든 사용자가 접근 가능하도록 설정
-- 프로덕션 환경에서는 RLS를 활성화하고 적절한 정책을 설정하세요.
ALTER TABLE posts ENABLE ROW LEVEL SECURITY;

-- 개발용 정책: 모든 사용자가 읽기/쓰기 가능 (개발 환경용)
-- 프로덕션에서는 인증된 사용자만 접근하도록 변경하세요.
-- 기존 정책이 있으면 삭제 후 재생성
DROP POLICY IF EXISTS "Allow all operations for development" ON posts;
CREATE POLICY "Allow all operations for development" 
ON posts FOR ALL 
USING (true) 
WITH CHECK (true);

-- 테이블 생성 확인
SELECT 'posts 테이블이 성공적으로 생성되었습니다!' AS message;
SELECT * FROM posts LIMIT 0;

