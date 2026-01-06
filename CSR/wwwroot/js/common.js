
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