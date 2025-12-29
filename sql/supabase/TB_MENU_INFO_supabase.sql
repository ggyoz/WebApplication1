-- TB_MENU_INFO Table (MENU 정보) for Supabase (PostgreSQL)

CREATE TABLE public.TB_MENU_INFO (
    MENUID          varchar(40)     NOT NULL,
    SYSTEMCODE      varchar(10)     DEFAULT 'CSR' NOT NULL,
    MENUNAME        varchar(100)    NOT NULL,
    CONTROLLER      varchar(20),
    ACTION          varchar(20),
    URL             varchar(300),
    PARENTID        varchar(40), -- Made nullable for root items
    INFO            varchar(1000),
    SORTORDER       integer,
    USEYN           varchar(1)      DEFAULT 'Y' NOT NULL,
    REG_DATE        timestamptz     DEFAULT now() NOT NULL,
    REG_USERID      varchar(10)     NOT NULL,
    UPDATE_DATE     timestamptz,
    UPDATE_USERID   varchar(10),
    CONSTRAINT PK_TB_MENU_INFO PRIMARY KEY (MENUID)
);

-- Comments
COMMENT ON TABLE public.TB_MENU_INFO IS 'MENU 정보 테이블';

COMMENT ON COLUMN public.TB_MENU_INFO.MENUID IS '메뉴 ID';
COMMENT ON COLUMN public.TB_MENU_INFO.SYSTEMCODE IS '시스템 코드';
COMMENT ON COLUMN public.TB_MENU_INFO.MENUNAME IS '메뉴 이름';
COMMENT ON COLUMN public.TB_MENU_INFO.CONTROLLER IS '메뉴 컨트롤러';
COMMENT ON COLUMN public.TB_MENU_INFO.ACTION IS '메뉴 행동';
COMMENT ON COLUMN public.TB_MENU_INFO.URL IS '메뉴 경로';
COMMENT ON COLUMN public.TB_MENU_INFO.PARENTID IS '상위 메뉴 ID';
COMMENT ON COLUMN public.TB_MENU_INFO.INFO IS '메뉴정보';
COMMENT ON COLUMN public.TB_MENU_INFO."ORDER" IS '메뉴 정렬 순서';
COMMENT ON COLUMN public.TB_MENU_INFO.USEYN IS '사용 여부';
COMMENT ON COLUMN public.TB_MENU_INFO.REG_DATE IS '등록일';
COMMENT ON COLUMN public.TB_MENU_INFO.REG_USERID IS '등록자 ID';
COMMENT ON COLUMN public.TB_MENU_INFO.UPDATE_DATE IS '수정일';
COMMENT ON COLUMN public.TB_MENU_INFO.UPDATE_USERID IS '수정자 ID';

-- Row Level Security (RLS)
ALTER TABLE public.TB_MENU_INFO ENABLE ROW LEVEL SECURITY;

-- Policies
CREATE POLICY "Allow all for authenticated users"
ON public.TB_MENU_INFO
FOR ALL
TO authenticated
USING (true);

CREATE POLICY "Enable read access for all users"
ON public.TB_MENU_INFO
FOR SELECT
USING (true);