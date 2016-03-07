#pragma once

class CFeatureMap;
class CFeatureList;
class CNamedVarSet;
typedef CNamedVarSet CState;
typedef CNamedVarSet CAction;
class CParameters;

#include "parameterized-object.h"
//CLinearVFA////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////

class CLinearVFA
{
protected:

	double* m_pWeights;
	unsigned int m_numWeights;

	bool m_bSaturateOutput;
	double m_minOutput, m_maxOutput;

public:
	CLinearVFA();
	~CLinearVFA();
	double getValue(const CFeatureList *features);
	double *getWeightPtr(){ return m_pWeights; }
	unsigned int getNumWeights(){ return m_numWeights; }

	void saturateOutput(double min, double max);

	void save(void* pFile);
	void load(void* pFile);
};

class CLinearStateVFA: public CParamObject, public CLinearVFA
{
protected:
	CFeatureMap* m_pStateFeatureMap;
	CFeatureList *m_pAux;

public:

	CLinearStateVFA(CParameters* pParameters);
	~CLinearStateVFA();
	using CLinearVFA::getValue;
	double getValue(const CState *s);

	void getFeatures(const CState* s,CFeatureList* outFeatures);
	void getFeatureState(unsigned int feature, CState* s);
	void add(const CFeatureList* pFeatures,double alpha= 1.0);
};


class CLinearStateActionVFA : public CParamObject, public CLinearVFA
{
protected:

	CFeatureMap* m_pStateFeatureMap;
	CFeatureMap* m_pActionFeatureMap;
	unsigned int m_numStateWeights;
	unsigned int m_numActionWeights;

	CFeatureList *m_pAux;
	CFeatureList *m_pAux2;

public:

	CLinearStateActionVFA(CParameters* pParameters);
	~CLinearStateActionVFA();
	using CLinearVFA::getValue;
	double getValue(const CState *s, const CAction *a);

	void argMax(const CState *s, CAction* a);

	void getFeatures(const CState* s, const CAction* a, CFeatureList* outFeatures);

	//features are built using the two feature maps: the state and action feature maps
	//
	void getFeatureStateAction(unsigned int feature, CState* s, CAction* a);

	void add(const CFeatureList* pFeatures, double alpha = 1.0);
};
