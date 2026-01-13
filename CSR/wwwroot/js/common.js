
function formatPhoneNumber(phoneNumber) {
  // 숫자 이외의 문자 제거 (Lodash의 _.replace 대신 순수 JS replace 사용)
  let formatted = '';
  formatted = phoneNumber.replace(/[^0-9]/g, '').replace(/^(\d{3})(\d{3,4})(\d{4})$/, `$1-$2-$3`);

  return formatted;
}

function NumberOnly(text){
  let formatted = '';
  formatted = text.replace(/[^0-9]/g, '');
  
  return formatted;
}

/**
 * DevExtreme HTML 에디터를 초기화하고, 숨겨진 입력 필드 및 미리보기와 연결합니다.
 * @param {string} editorSelector - HTML 에디터로 사용할 div의 CSS 선택자 (예: '.html-editor')
 * @param {string} inputSelector - 에디터의 값을 저장할 숨겨진 input의 CSS 선택자 (예: 'input[name="CONTENTS_HTML"]')
 * @param {string} previewSelector - 실시간 미리보기를 표시할 요소의 CSS 선택자 (예: '.value-content')
 */
function initializeHtmlEditor(editorSelector, inputSelector, previewSelector) {
    // 1. 초기값 설정: 숨겨진 input에서 값을 가져와 에디터의 초기 값으로 사용합니다.
    const initialValue = $(inputSelector).val();

    // 2. dxHtmlEditor 인스턴스 생성
    const editorInstance = $(editorSelector).dxHtmlEditor({
        height: 300,
        value: initialValue,
        toolbar: {
            items: [
                'undo', 'redo', 'separator',
                'bold', 'italic', 'separator',
                {
                    name: 'header',
                    acceptedValues: [false, 1, 2, 3, 4, 5],
                    options: { inputAttr: { 'aria-label': 'Header' } },
                },
                'separator',
                'orderedList', 'bulletList', 'separator',
                'link', 'image', // 링크 및 이미지 추가 기능
            ],
        },
        // 3. 값이 변경될 때마다 숨겨진 input과 미리보기 영역을 업데이트합니다.
        onValueChanged(e) {
            $(inputSelector).val(e.value);
            if (previewSelector) {
                $(previewSelector).html(e.value);
            }
        },
    }).dxHtmlEditor('instance');

    // 4. 초기 미리보기 내용을 설정합니다.
    if (previewSelector) {
        $(previewSelector).html(initialValue);
    }
}