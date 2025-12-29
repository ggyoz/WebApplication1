-- TB_COR_INFO Table (법인 정보 테이블) for Supabase (PostgreSQL)

CREATE TABLE public.TB_COR_INFO (
    CORCD       varchar(10)     NOT NULL,
    CORNM       varchar(100),
    NATIONCD    varchar(10),
    COINCD      varchar(10),
    "LANGUAGE"  varchar(10),
    ACC_TITLE   varchar(10),
    CONSTRAINT PK_TB_COR_INFO PRIMARY KEY (CORCD)
);

-- Comments
COMMENT ON TABLE public.TB_COR_INFO IS '법인 정보 테이블';

COMMENT ON COLUMN public.TB_COR_INFO.CORCD IS '법인코드';
COMMENT ON COLUMN public.TB_COR_INFO.CORNM IS '법인명';
COMMENT ON COLUMN public.TB_COR_INFO.NATIONCD IS '국가코드';
COMMENT ON COLUMN public.TB_COR_INFO.COINCD IS '통화코드';
COMMENT ON COLUMN public.TB_COR_INFO."LANGUAGE" IS '언어코드';
COMMENT ON COLUMN public.TB_COR_INFO.ACC_TITLE IS '계정과목';

-- Row Level Security (RLS)
ALTER TABLE public.TB_COR_INFO ENABLE ROW LEVEL SECURITY;

-- Policies
CREATE POLICY "Allow all for authenticated users"
ON public.TB_COR_INFO
FOR ALL
TO authenticated
USING (true);

CREATE POLICY "Enable read access for all users"
ON public.TB_COR_INFO
FOR SELECT
USING (true);