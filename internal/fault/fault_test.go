// 单元测试：容错模块
package fault

import (
	"errors"
	"testing"
	"github.com/stretchr/testify/assert"
)

func TestStandardizeError_Normal(t *testing.T) {
	err := errors.New("test error")
	fe := StandardizeError(err)
	assert.NotNil(t, fe)
	assert.Equal(t, "test error", fe.Message)
}

func TestStandardizeError_Nil(t *testing.T) {
	fe := StandardizeError(nil)
	assert.Nil(t, fe)
}

// 测试默认容错处理器
func TestDefaultFaultHandler_DoWithFaultTolerance_Normal(t *testing.T) {
	handler := &DefaultFaultHandler{}
	
	res, fe := handler.DoWithFaultTolerance(func() (interface{}, error) {
		return "ok", nil
	}, nil, nil)
	assert.NotNil(t, res)
	assert.Nil(t, fe)
}

func TestDefaultFaultHandler_DoWithFaultTolerance_Error(t *testing.T) {
	handler := &DefaultFaultHandler{}
	
	res, fe := handler.DoWithFaultTolerance(func() (interface{}, error) {
		return nil, errors.New("fail")
	}, nil, nil)
	assert.Nil(t, res)
	assert.NotNil(t, fe)
	assert.Equal(t, "DOWNSTREAM_ERROR", fe.Code)
}