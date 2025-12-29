-- TB_USER_INFO Table (유저 정보 테이블) for Supabase (PostgreSQL)

CREATE TABLE public.TB_USER_INFO (
    USERID          varchar(10)     NOT NULL,
    USERPWD         varchar(100),
    USERNAME        varchar(100),
    EMPNO           varchar(10),
    CORCD           varchar(10),
    DEPTCD          varchar(10),
    OFFICECD        varchar(10),
    TEAMCD          varchar(10),
    SYSCD           varchar(10),
    BIZCD           varchar(10),
    TELNO           varchar(20),
    MOB_PHONE_NO    varchar(20),
    EMAIL_ADDR      varchar(30),
    STATUS          varchar(10),
    RETIRE_DATE     timestamptz,
    ADMIN_FLAG      integer         DEFAULT 0,
    CUSTCD          varchar(10),
    VENDCD          varchar(10),
    AUTH_FLAG       integer         DEFAULT 0,
    USER_DIV        varchar(10),
    PW_MISS_COUNT   integer,
    REG_DATE        timestamptz     DEFAULT now() NOT NULL,
    REG_USERID      varchar(10)     NOT NULL,
    UPDATE_DATE     timestamptz,
    UPDATE_USERID   varchar(10),
    USEYN           varchar(1)     DEFAULT 'Y' NOT NULL,
    CONSTRAINT PK_TB_USER_INFO PRIMARY KEY (USERID)
);

-- Comments
COMMENT ON TABLE public.TB_USER_INFO IS '유저 정보 테이블';

COMMENT ON COLUMN public.TB_USER_INFO.USERID IS '사용자 ID';
COMMENT ON COLUMN public.TB_USER_INFO.USERPWD IS '사용자 비밀번호';
COMMENT ON COLUMN public.TB_USER_INFO.USERNAME IS '사용자 이름';
COMMENT ON COLUMN public.TB_USER_INFO.EMPNO IS '사원 번호';
COMMENT ON COLUMN public.TB_USER_INFO.CORCD IS '법인 코드';
COMMENT ON COLUMN public.TB_USER_INFO.DEPTCD IS '사업부 코드';
COMMENT ON COLUMN public.TB_USER_INFO.OFFICECD IS '실 코드';
COMMENT ON COLUMN public.TB_USER_INFO.TEAMCD IS '팀 코드';
COMMENT ON COLUMN public.TB_USER_INFO.SYSCD IS '시스템 코드';
COMMENT ON COLUMN public.TB_USER_INFO.BIZCD IS '사업장 코드';
COMMENT ON COLUMN public.TB_USER_INFO.TELNO IS '전화 번호';
COMMENT ON COLUMN public.TB_USER_INFO.MOB_PHONE_NO IS '휴대폰 번호';
COMMENT ON COLUMN public.TB_USER_INFO.EMAIL_ADDR IS '전자메일 주소';
COMMENT ON COLUMN public.TB_USER_INFO.STATUS IS '계정 상태';
COMMENT ON COLUMN public.TB_USER_INFO.RETIRE_DATE IS '퇴사일';
COMMENT ON COLUMN public.TB_USER_INFO.ADMIN_FLAG IS '관리자권한';
COMMENT ON COLUMN public.TB_USER_INFO.CUSTCD IS '고객사 코드';
COMMENT ON COLUMN public.TB_USER_INFO.VENDCD IS '협력사 코드';
COMMENT ON COLUMN public.TB_USER_INFO.AUTH_FLAG IS '메뉴부여권한';
COMMENT ON COLUMN public.TB_USER_INFO.USER_DIV IS '사용자구분(CLSSID=''T1'')';
COMMENT ON COLUMN public.TB_USER_INFO.PW_MISS_COUNT IS '로그인 실패 횟수';
COMMENT ON COLUMN public.TB_USER_INFO.REG_DATE IS '등록일';
COMMENT ON COLUMN public.TB_USER_INFO.REG_USERID IS '등록자 ID';
COMMENT ON COLUMN public.TB_USER_INFO.UPDATE_DATE IS '수정일';
COMMENT ON COLUMN public.TB_USER_INFO.UPDATE_USERID IS '수정자 ID';
COMMENT ON COLUMN public.TB_TEAM_INFO.USEYN IS '사용여부';

-- Row Level Security (RLS)
ALTER TABLE public.TB_USER_INFO ENABLE ROW LEVEL SECURITY;

-- Policies
CREATE POLICY "Allow all for authenticated users"
ON public.TB_USER_INFO
FOR ALL
TO authenticated
USING (true);

CREATE POLICY "Enable read access for all users"
ON public.TB_USER_INFO
FOR SELECT
USING (true);
