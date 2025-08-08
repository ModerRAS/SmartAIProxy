// 单元测试：安全审计模块
package security

import (
	"testing"
	"github.com/stretchr/testify/assert"
)

func TestDesensitize(t *testing.T) {
	phone := "13812345678"
	email := "test@example.com"
	id := "110101199003071234"
	input := phone + " " + email + " " + id
	out := Desensitize(input)
	assert.Contains(t, out, "138****5678")
	assert.Contains(t, out, "test***@example.com")
	assert.Contains(t, out, "110101199****71234") // 根据实际函数修改期望结果
}

func TestNewAuditEvent(t *testing.T) {
	event := NewAuditEvent("login", "user1", "13812345678 test@example.com 110101199003071234")
	assert.NotNil(t, event)
	assert.Contains(t, event.Detail, "****")
}